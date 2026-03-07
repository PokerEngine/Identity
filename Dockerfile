FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /usr/local/identity

CMD ["dotnet", "watch", "--project", "Infrastructure", "run"]
