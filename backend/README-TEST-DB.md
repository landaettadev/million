# Test Database (End-to-End)

This setup provides a deterministic MongoDB dataset to run full end-to-end tests against the API.

## Requirements
- Docker Desktop (or compatible engine)

## Start Mongo + Seed

Run from repo root:

`
cd backend
# Start Mongo on host port 27018 and import seed data
docker compose -f docker-compose.test.yml up -d --wait
`

This will:
- Start mongo:7 on localhost:27018 with database realestate_test
- Import seed collections from ./seed/test/*.json:
  - owners, properties, propertyImages, propertyTraces

## Configure API to use test DB
Run the API pointing to this database:

`
# Powershell / cmd from repo root
set ASPNETCORE_URLS=http://localhost:5244
set MongoDb__ConnectionString=mongodb://localhost:27018
set MongoDb__Database=realestate_test
set SWAGGER_ENABLED=false
set CORS_ALLOWED_ORIGINS=http://localhost:3000

cd backend/RealEstate.Api
dotnet run
`

## Verify endpoints
- Health: GET http://localhost:5244/health
- List: GET http://localhost:5244/api/properties
- Filter: GET http://localhost:5244/api/properties?name=Test&minPrice=2000000
- Detail: GET http://localhost:5244/api/properties/66f0a0000000000000000001

## Stop and clean

`
docker compose -f docker-compose.test.yml down -v
`
