FROM microsoft/dotnet:2.1-sdk AS publish
WORKDIR /src
COPY NuGet.Config .
COPY Server/Server.csproj Server/Server.csproj
RUN dotnet restore Server
COPY . .
RUN dotnet publish Server -c Release -o /app

FROM microsoft/dotnet:2.1-aspnetcore-runtime AS final
EXPOSE 80 443
WORKDIR /app
ENTRYPOINT ["dotnet", "/app/Server.dll"]
COPY --from=publish /app .
