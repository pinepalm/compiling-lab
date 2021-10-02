FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY ./ /app/
WORKDIR /app/BUAA.CodeAnalysis.SysY/
RUN ls -a
RUN dotnet build --configuration Release
WORKDIR /app/BUAA.CodeAnalysis.SysY/bin/Release/net5.0/
RUN ls -a
