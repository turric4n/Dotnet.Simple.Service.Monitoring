# Dotnet.Simple.Service.Monitoring

Avoid complex health check implementations... Just add some lines in your settings and find out when your services are falling down with awesome monitoring.

This chunk of code, basically is known (colloquially) as a wannabe library/framework/wrapper to simplify .NET health checks framework.

It uses .NET HealthChecks engine and awesome Xabaril's AspNetCore.HealthChecks - https://github.com/xabaril/AspNetCore.Diagnostics.HealthChecks

This code is just doing the same thing but in a very easy way... BUT also publishes health check results into multiple communication providers (Email, Telegram, Slack...)

*HOW IT WORKS?*
Just put some lines in your settings file and MAGIC! Health checks and transport providers will be automated.

## Initialization code : 

Startup.cs

```
  var monitoring = services.UseServiceMonitoring(Configuration)
      .UseSettings()
      .Build();
```

## Settings :

appSettings.json

```
{
  "Monitoring": {
    "HealthChecks": [
      {
        "Name": "Test",
        "ServiceType": "Http",
        "EndpointOrHost": "https://www.testendpoint.com/",
        "Port": 443,
        "HealthCheckConditions": {
          "HttpBehaviour": {
            "HttpExpectedResponseTimeMs": 300,
            "HttpExpectedCode": 200,
            "HttpVerb": "Get"
          },
          "ServiceReach": true,
          "ServiceConnectionEstablished": true
        },
        "PublishChecks": true,
        "Alert": true,
        "AlertBehaviour": [
          {
            "TransportMethod": "Email",
            "TransportName": "StandardEmailTransport",
            "AlertOnce": true,
            "AlertOnServiceRecovered": true,
            "StartAlertingOn": "",
            "StopAlertingOn": "",
            "AlertEvery": "00:00:05",
            "AlertOn": ""
          }
        ]
      }
    ],
    "EmailTransportSettings": [
      {
        "Name": "StandardEmailTransport",
        "From": "test@test.com",
        "To": "test@test.com",
        "SmtpHost": "test.test.com",
        "SmtpPort": 25,
        "Authentication": false,
        "Username": "",
        "Password": "",
        "Template": "Plain"
      }
    ]
  }
}
```

Or use appsettings.yml

```
Monitoring:
  HealthChecks:
  - Name: Test
    ServiceType: Http
    EndpointOrHost: https://www.testendpoint.com/
    Port: 443
    HealthCheckConditions:
      HttpBehaviour:
        HttpExpectedResponseTimeMs: 300
        HttpExpectedCode: 200
        HttpVerb: Get
      ServiceReach: true
      ServiceConnectionEstablished: true
    PublishChecks: true
    Alert: true
    AlertBehaviour:
    - TransportMethod: Email
      TransportName: StandardEmailTransport
      AlertOnce: true
      AlertOnServiceRecovered: true
      StartAlertingOn: ''
      StopAlertingOn: ''
      AlertEvery: '00:00:05'
      AlertOn: ''
  EmailTransportSettings:
  - Name: StandardEmailTransport
    From: test@test.com
    To: test@test.com
    SmtpHost: test.test.com
    SmtpPort: 25
    Authentication: false
    Username: ''
    Password: ''
    Template: Plain    
 ```
 
 ## Monitoring properties :
 - HealthChecks (Array)
   - Name (Health check unique name)
   - Service Type (Type of service you want to monitor) (Enum)
     - Http (Typical http or https URI)
     - ElasticSearch (ElasticSearch endpoint is supported)
     - MsSql (Microsoft SQL Server)
     - Rmq (Rabbit MQ)
     - Hangfire
     - Ping (ICMP)
   - EndpointOrHost (Mandatory when monitoring HTTP or ElasticSearch, just an URI)
   - PublishChecks (Enable check publishing, disable when you don't want to hear anything from this check)
   - Alert (Enable when you want to publish checks into implicated transport methods)
   - AlertBehaviour (Define here transports you will use to publish health check results) (Object)
     - TransportMethod (Array) (Transport methods you want to use)
      - Email
      - Slack
      - Telegram
      - InfluxDb
     - TransportName (Name of the defined transport in the config file, go to transport section)
     - AlertOnce (Just send one notification on health check failure)
     - AlertOnServiceRecovered (Send notification when health check is recovered)
     - StartAlertingOn (Start to send alerts on suggested DateTime)
     - StopAlertingOn (Stop sending alerts on suggested DateTime)            
     - AlertEvery (Time between alerts)
 
## Transport properties :
- EmailTransportSettings (Array) (Object)
  - Name (Transport unique name)
    
(TBC)
 
