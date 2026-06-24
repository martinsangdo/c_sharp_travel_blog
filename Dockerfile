FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY TravelBlog.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish TravelBlog.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p wwwroot/images/uploads/blogs wwwroot/images/uploads/avatars
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TravelBlog.dll"]
