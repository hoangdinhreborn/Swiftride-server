# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src
COPY . .
RUN dotnet publish "$PROJECT" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG DLL
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    APP_DLL=$DLL
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["sh", "-c", "exec dotnet $APP_DLL"]