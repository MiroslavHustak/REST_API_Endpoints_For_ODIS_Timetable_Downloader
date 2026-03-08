FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
RUN mkdir -p CanopyResults jsonLinks logging
CMD ["dotnet", "REST_API_Endpoints_For_ODIS_Timetable_Downloader.dll"]