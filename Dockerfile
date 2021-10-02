FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY ./ /app/
WORKDIR /app/TestSubmit/
RUN ls -a
RUN dotnet build --configuration Release
WORKDIR /app/TestSubmit/bin/Release/net5.0/
RUN ls -a
