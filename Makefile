.PHONY: build run test cov
build: ; dotnet build
run: ; dotnet run --project api
test: ; dotnet test ./api.tests -v minimal
cov: ; dotnet test ./api.tests -v minimal /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
