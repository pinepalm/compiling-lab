FROM mcr.microsoft.com/dotnet/runtime:5.0
COPY ./* /app/
WORKDIR /app/TestSubmit/
RUN dotnet build --configuration Release
WORKDIR /app/TestSubmit/bin/Release/net5.0/
