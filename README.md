# Dotnet.Simple.Service.Monitoring

Avoid hard healthcheck implementations... Just add some lines in your settings and enjoy about monitoring.

This is a library, framework, wrapper to simplify .NET health checks framework. It uses .NET HealthChecks engine and Xabaril's AspNetCore.HealthChecks https://github.com/xabaril/AspNetCore.Diagnostics.HealthChecks

Basically this library is just doing the same thing but in very easy way, also publishes health check results into multiple communication providers (Email, Telegram, Slack...)

The way to do it -> Just put some lines in your settings and that will automate health checks and publish results.

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
        "From": "turrican@habitatsoft.com",
        "To": "turrican@hotmail.com",
        "SmtpHost": "turbomail.habitatsoft.com",
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
    From: turrican@habitatsoft.com
    To: turrican@hotmail.com
    SmtpHost: turbomail.habitatsoft.com
    SmtpPort: 25
    Authentication: false
    Username: ''
    Password: ''
    Template: Plain    
 ```
