#!/usr/bin/env bash
# Chạy cả luồng: đặt xe -> nhận -> đón -> trả -> (qua RabbitMQ) thanh toán -> Paid
set -euo pipefail

GW=${GATEWAY_URL:-http://localhost:5000}
H='Content-Type: application/json'
BODY='{"pickup":"Ben Thanh","pickupLat":10.7721,"pickupLng":106.6980,"destination":"Phu My Hung","destinationLat":10.7290,"destinationLng":106.7190}'

tok() { curl -sf -X POST "$GW/dev/token" -H "$H" -d "{\"role\":\"$1\"}"; }
code() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
expect() { [ "$1" = "$2" ] || { echo "FAIL: $3 (nhan $1, can $2)"; exit 1; }; echo "ok: $3"; }

RIDER=$(tok Rider); DRIVER=$(tok Driver)

expect "$(code -X POST "$GW/trips" -H "$H" -d "$BODY")" 401 "khong token bi chan o gateway"
expect "$(code -X POST "$GW/trips" -H "Authorization: Bearer $DRIVER" -H "$H" -d "$BODY")" 403 "driver khong dat xe duoc"

ID=$(curl -sf -X POST "$GW/trips" -H "Authorization: Bearer $RIDER" -H "$H" -d "$BODY" | grep -o '"id":"[^"]*"' | head -n1 | cut -d'"' -f4)
[ -n "$ID" ] || { echo "FAIL: khong tao duoc chuyen"; exit 1; }
echo "ok: tao chuyen $ID (gateway -> trip -> matching)"

for step in driver-accept pickup dropoff; do
  expect "$(code -X POST "$GW/trips/$ID/$step" -H "Authorization: Bearer $DRIVER")" 200 "$step"
done

for _ in $(seq 1 30); do   # outbox quet moi 2 giay, cho toi 60 giay
  STATUS=$(curl -sf "$GW/trips/$ID" -H "Authorization: Bearer $RIDER" | grep -o '"status":"[^"]*"' | cut -d'"' -f4 || true)
  if [ "$STATUS" = "Paid" ]; then echo "ok: chuyen da Paid (trip -> rabbitmq -> payment -> rabbitmq -> trip)"; exit 0; fi
  sleep 2
done
echo "FAIL: chuyen khong len Paid, trang thai cuoi: ${STATUS:-?}"; exit 1