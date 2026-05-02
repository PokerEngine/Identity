FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /usr/local/identity

CMD ["dotnet", "run", "--project", "Api", "--no-launch-profile"]
