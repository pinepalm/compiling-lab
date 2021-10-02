FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY ./ /app/
WORKDIR /app/SysYLexer/
RUN ls -a
RUN dotnet build --configuration Release
WORKDIR /app/SysYLexer/bin/Release/net5.0/
RUN ls -a
