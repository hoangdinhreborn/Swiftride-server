#!/usr/bin/env bash
# Chạy 3 kịch bản theo flowchart: (1) đủ luồng, tài xế đầu từ chối, (2) rider từ chối giá, (3) tài xế từ chối quá k lần
set -euo pipefail

GW=${GATEWAY_URL:-http://localhost:5000}
H='Content-Type: application/json'

tok()    { curl -sf -X POST "$GW/dev/token" -H "$H" -d "{\"role\":\"$1\"}"; }
code()   { curl -s -o /dev/null -w '%{http_code}' "$@"; }
field()  { grep -o "\"$1\":\"[^\"]*\"" | sed -n 1p | cut -d'"' -f4; }
plus()   { awk -v b="$1" -v d="$2" 'BEGIN{printf "%.4f", b+d}'; }
fail()   { echo "FAIL: $1"; exit 1; }
expect() { [ "$1" = "$2" ] || fail "$3 (nhan $1, can $2)"; echo "ok: $3"; }

body()       { echo "{\"pickup\":\"Ben Thanh\",\"pickupLat\":$1,\"pickupLng\":106.6980,\"destination\":\"Phu My Hung\",\"destinationLat\":$(plus "$1" 0.03),\"destinationLng\":106.7190}"; }
place()      { expect "$(code -X PUT "$GW/drivers/me/location" -H "Authorization: Bearer $1" -H "$H" -d "{\"lat\":$2,\"lng\":106.6980,\"isAvailable\":true}")" 200 "tai xe san sang tai lat $2"; }
new_trip()   { curl -sf -X POST "$GW/trips" -H "Authorization: Bearer $RIDER" -H "$H" -d "$(body "$1")" | field id; }
status_of()  { curl -sf "$GW/trips/$1" -H "Authorization: Bearer $RIDER" | field status; }
post()       { code -X POST "$GW/trips/$2/$3" -H "Authorization: Bearer $1"; }   # post TOKEN TRIP_ID ACTION
has_offer()  { local o; o=$(curl -sf "$GW/trips/offers" -H "Authorization: Bearer $1") || return 1; [[ "$o" == *"$2"* ]]; }

RUN_LAT=$(awk -v r="$RANDOM" 'BEGIN{printf "%.4f", 8 + r/32768*3}')
RIDER=$(tok Rider); A=$(tok Driver); B=$(tok Driver)
echo "Khu vuc test: lat $RUN_LAT"

echo "== 1. Du luong, tai xe dau tien tu choi, tai xe thu hai nhan"
expect "$(code -X POST "$GW/trips" -H "$H" -d "$(body "$RUN_LAT")")" 401 "khong token bi chan o gateway"
expect "$(code -X POST "$GW/trips" -H "Authorization: Bearer $A" -H "$H" -d "$(body "$RUN_LAT")")" 403 "driver khong dat xe duoc"

place "$A" "$(plus "$RUN_LAT" 0.0001)"
place "$B" "$(plus "$RUN_LAT" 0.0010)"

ID=$(new_trip "$RUN_LAT"); [ -n "$ID" ] || fail "khong tao duoc chuyen"
expect "$(status_of "$ID")" Requested "bao gia xong, cho rider xac nhan (gateway -> trip -> matching)"
expect "$(post "$A" "$ID" confirm)" 403 "tai xe khong xac nhan gia duoc"
expect "$(post "$RIDER" "$ID" confirm)" 200 "rider dong y gia"
expect "$(status_of "$ID")" DriverOffered "da de xuat cho tai xe"
has_offer "$A" "$ID" || fail "tai xe gan nhat phai thay de xuat"
if has_offer "$B" "$ID"; then fail "tai xe xa hon khong duoc thay de xuat"; fi
expect "$(post "$B" "$ID" driver-accept)" 403 "tai xe khong duoc de xuat khong nhan duoc"

expect "$(post "$A" "$ID" driver-decline)" 200 "tai xe A tu choi"
has_offer "$B" "$ID" || fail "sau khi A tu choi phai de xuat cho B"
expect "$(post "$A" "$ID" driver-accept)" 403 "A da tu choi nen khong nhan lai duoc"

for step in driver-accept pickup dropoff; do
  expect "$(post "$B" "$ID" "$step")" 200 "tai xe B: $step"
done

STATUS=""
for _ in $(seq 1 30); do   # outbox quet moi 2 giay, cho toi 60 giay
  STATUS=$(status_of "$ID" || true)
  [ "$STATUS" = "Paid" ] && break
  sleep 2
done
expect "$STATUS" Paid "chuyen da Paid (trip -> rabbitmq -> payment -> rabbitmq -> trip)"

echo "== 2. Rider tu choi gia"
ID2=$(new_trip "$RUN_LAT"); [ -n "$ID2" ] || fail "khong tao duoc chuyen"
expect "$(post "$RIDER" "$ID2" cancel)" 200 "rider tu choi gia"
expect "$(status_of "$ID2")" Cancelled "ket thuc: huy chuyen"
expect "$(post "$RIDER" "$ID2" confirm)" 409 "chuyen da huy khong xac nhan lai duoc"

echo "== 3. Tai xe tu choi qua k lan"
LAT3=$(plus "$RUN_LAT" 1)   # khu vuc khac, khong lan voi tai xe o kich ban 1
declare -a DT
for i in 1 2 3 4; do DT[$i]=$(tok Driver); place "${DT[$i]}" "$(plus "$LAT3" "0.000$i")"; done

ID3=$(new_trip "$LAT3"); [ -n "$ID3" ] || fail "khong tao duoc chuyen"
expect "$(post "$RIDER" "$ID3" confirm)" 200 "rider dong y gia"
for round in 1 2 3 4; do
  OFFERED=""
  for i in 1 2 3 4; do
    if has_offer "${DT[$i]}" "$ID3"; then OFFERED=$i; break; fi
  done
  [ -n "$OFFERED" ] || fail "vong $round: khong tai xe nao duoc de xuat"
  expect "$(post "${DT[$OFFERED]}" "$ID3" driver-decline)" 200 "tai xe $OFFERED tu choi (lan $round)"
done
expect "$(status_of "$ID3")" Failed "tu choi qua k lan -> ket thuc khong thanh cong"

echo "TAT CA OK"