# Dotnet.Simple.Service.Monitoring

Simplify Health Check Implementations with Easy Monitoring

Forget about complex health check implementations. With just a few lines added to your settings, you can effortlessly monitor the status of your services using an amazing monitoring solution.

This code snippet, often referred to colloquially as a wannabe library/framework/wrapper, aims to simplify the .NET health checks framework. It leverages the power of the .NET HealthChecks engine and the fantastic AspNetCore.HealthChecks library by Xabaril (available at https://github.com/xabaril/AspNetCore.Diagnostics.HealthChecks).

By utilizing this code, you can achieve the same functionality with ease. Additionally, it provides the capability to publish health check results to various communication providers, including Email, Telegram, and Slack.

How does it work? Simply add a few lines to your settings file, and voila! Health checks and transport providers will be automatically configured.

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
 
- **HealthChecks (Array)**
  - **Name:** Health check unique name
  - **Service Type:** Type of service you want to monitor (Enum)
    - **Http:** Typical http or https URI
    - **ElasticSearch:** ElasticSearch endpoint is supported
    - **MsSql:** Microsoft SQL Server
    - **Rmq:** Rabbit MQ
    - **Hangfire**
    - **Ping:** ICMP
  - **EndpointOrHost:** Mandatory when monitoring HTTP or ElasticSearch, just an URI
  - **PublishChecks:** Enable check publishing, disable when you don't want to hear anything from this check
  - **Alert:** Enable when you want to publish checks into implicated transport methods
  - **AlertBehaviour (Object):**
    - **TransportMethod (Array):** Transport methods you want to use
      - **Email**
      - **Slack**
      - **Telegram**
      - **InfluxDb**
    - **TransportName:** Name of the defined transport in the config file, go to transport section
    - **AlertOnce:** Just send one notification on health check failure
    - **AlertOnServiceRecovered:** Send notification when health check is recovered
    - **StartAlertingOn:** Start to send alerts on suggested DateTime
    - **StopAlertingOn:** Stop sending alerts on suggested DateTime
    - **AlertEvery:** Time between alerts
 
## Transport properties :
- EmailTransportSettings (Array) (Object)
  - Name (Transport unique name)
    
(TBC)
 
