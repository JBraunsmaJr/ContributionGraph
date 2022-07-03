FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Github.Actions.ContributionGraph/Github.Actions.ContributionGraph.csproj", "Github.Actions.ContributionGraph/"]
COPY ["Github.Actions.Core/Github.Actions.Core.csproj", "Github.Actions.Core/"]
RUN dotnet restore "Github.Actions.ContributionGraph/Github.Actions.ContributionGraph.csproj"
COPY . .

WORKDIR "/src/Github.Actions.ContributionGraph"
RUN dotnet build "Github.Actions.ContributionGraph.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Github.Actions.ContributionGraph.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Github.Actions.ContributionGraph.dll"]
