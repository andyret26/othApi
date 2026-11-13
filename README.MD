### for docker compose 

Create a secrets.json file in the root folder with the values below
{
  "JWKS_URI": "JWKS_URI",
  "AUTOMAPPER_LICENCE_KEY": "AutomapperLicenceKey",
  "OSU_CLIENT_ID": "OsuClientId",
  "OSU_CLIENT_SECRET": "OsuClientSecret",
  "OSU_API_V1_KEY": "OsuApiV1Key",
  "DISCORD_BOT_TOKEN": "DiscordBotToken"
}


from the root run "docker compose up"

then "dotnet tool install --global dotnet-ef"
then "dotnet ef database update --connection "Host=localhost;Port=5434;Database=othdb;Username=postgres;Password=postgrespassword"

api should now be up and running on 127.0.0.1:5110/swagger
