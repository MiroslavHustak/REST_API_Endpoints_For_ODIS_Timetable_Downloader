FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
# RUN mkdir -p /data/CanopyResults /data/jsonLinks /data/logging
CMD ["dotnet", "REST_API_Endpoints_For_ODIS_Timetable_Downloader.dll"]