FROM mcr.microsoft.com/dotnet/sdk:9.0

RUN dotnet tool install -g pbm
ENV PATH="${PATH}:/root/.dotnet/tools"

WORKDIR /app
COPY . /app

RUN dotnet build