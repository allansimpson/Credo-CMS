/*
  Credo CMS — Azure resources for a single-church production deployment.

  Provisions:
    * App Service Plan (Linux)
    * App Service (the API; serves the SPA from wwwroot)
    * Azure SQL Server + Database
    * Storage Account + 'images' container
    * Azure SignalR Service
    * Application Insights (linked to the App Service)

  All connection strings and secrets surface as App Service application settings
  via the resources below, so the app can read them from `IConfiguration`
  without any extra wiring.

  The default tier sizing is appropriate for small-church scale; bump
  `appServiceSku` and `sqlSku` for production loads.
*/

param environmentName string = 'production'
param location string = resourceGroup().location

@description('Base name; resources are named "${baseName}-<resource>-<envSuffix>".')
param baseName string = 'credocms'

@description('Linux App Service Plan SKU.')
@allowed([ 'B1', 'B2', 'P0v3', 'P1v3', 'S1' ])
param appServiceSku string = 'S1'

@description('Azure SQL Database SKU.')
@allowed([ 'Basic', 'S0', 'S1', 'S2', 'GP_S_Gen5_2' ])
param sqlSku string = 'S0'

@description('SQL Server admin login.')
param sqlAdminLogin string

@description('SQL Server admin password.')
@secure()
param sqlAdminPassword string

@description('Allow Azure services to access SQL (recommended).')
param allowAzureIpsToSql bool = true

@description('Public DNS name for the App Service (defaults to "<baseName>-app-<envSuffix>.azurewebsites.net").')
param customDomain string = ''

var envSuffix = toLower(environmentName)
var appServiceName = '${baseName}-app-${envSuffix}'
var planName = '${baseName}-plan-${envSuffix}'
var sqlServerName = '${baseName}-sql-${envSuffix}'
var sqlDatabaseName = '${baseName}-db'
var storageName = toLower(replace('${baseName}st${envSuffix}', '-', ''))
var signalRName = '${baseName}-sigr-${envSuffix}'
var appInsightsName = '${baseName}-ai-${envSuffix}'
var lawName = '${baseName}-law-${envSuffix}'

resource law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: lawName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    WorkspaceResourceId: law.id
  }
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planName
  location: location
  sku: {
    name: appServiceSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }

  resource blob 'blobServices' = {
    name: 'default'

    resource imagesContainer 'containers' = {
      name: 'images'
      properties: { publicAccess: 'None' }
    }
  }
}

resource sqlServer 'Microsoft.Sql/servers@2024-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }

  resource db 'databases' = {
    name: sqlDatabaseName
    location: location
    sku: {
      name: sqlSku
    }
    properties: {
      collation: 'SQL_Latin1_General_CP1_CI_AS'
    }
  }

  resource fwAzure 'firewallRules' = if (allowAzureIpsToSql) {
    name: 'AllowAllAzureIps'
    properties: {
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

resource signalR 'Microsoft.SignalRService/SignalR@2024-04-01-preview' = {
  name: signalRName
  location: location
  sku: {
    name: 'Free_F1'
    tier: 'Free'
    capacity: 1
  }
  kind: 'SignalR'
  properties: {
    features: [
      { flag: 'ServiceMode', value: 'Default' }
      { flag: 'EnableConnectivityLogs', value: 'true' }
    ]
    cors: {
      allowedOrigins: [ '*' ]
    }
  }
}

resource app 'Microsoft.Web/sites@2024-04-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
      healthCheckPath: '/api/health'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'ConnectionStrings__DefaultConnection', value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabaseName};User Id=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;' }
        { name: 'ConnectionStrings__AzureSignalR', value: signalR.listKeys().primaryConnectionString }
        { name: 'Storage__BlobConnectionString', value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value};EndpointSuffix=core.windows.net' }
        { name: 'Storage__ImagesContainer', value: 'images' }
        { name: 'ApplicationInsights__ConnectionString', value: appInsights.properties.ConnectionString }
        { name: 'Authentication__Cookie__Name', value: '.CredoCms.Auth' }
        { name: 'PublicSite__BaseUrl', value: empty(customDomain) ? 'https://${appServiceName}.azurewebsites.net' : 'https://${customDomain}' }
        { name: 'Identity__DefaultAdminEmail', value: 'admin@example.com' }
        { name: 'Identity__DefaultAdminPassword', value: 'Ch@ngeMeOnFirstLogin!' }
      ]
    }
  }
}

output appServiceUrl string = 'https://${appServiceName}.azurewebsites.net'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output storageAccountName string = storage.name
output signalRConnectionString string = signalR.listKeys().primaryConnectionString
output applicationInsightsConnectionString string = appInsights.properties.ConnectionString
