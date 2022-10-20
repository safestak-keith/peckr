# peckr
Peckr is a .NET Global Tool and console app to periodically monitor logs/metrics from a range of sources at a specified polling interval. 
It can run endlessly, for a specified duration or terminate upon a success/failure condition. Optionally, it can also push all retrieved logs/metrics out to a range of supported sinks.

## Install the .NET Global Tool
```bash
dotnet tool install -g peckr
```
The command above will install peckr as a global tool available as part of the system's path.

## Running the .NET Global Tool
### Usage Overview
    USAGE: 
    
    peckr [--help] --monitorType <type> --sourceConnection <connection> [--durationToRunMinutes <minutes> (default: 10)] [--pollingIntervalMilliseconds <millis> (default: 5000)] [--sourceFilter <filter>] [--sourcePreviousSpanMinutes <minutes> (default: 5)] [--sourceTakeLimit <count> (default: 10)] [--sourceAppOrResourceId <appId>] [--primaryThresholdValue <value> (default: 0)] [--secondaryThresholdValue <value> (default: 0)] [--terminateWhenConditionMet <bool> (default: true)] [--cooldownWhenConditionMetMinutes <c> (default: 5)] [--sinkType <type>] [--sinkConnection <connection>]

    OPTIONS:

        --help or -h                                    display this list of options
        --durationToRunMinutes or -d <minutes>          duration in minutes to run the monitoring
        --pollingIntervalMilliseconds or -i <millis>    interval in milliseconds to poll the source
        --monitorType or -m <type>                      type of peckr (see Peckr Types below)
        --sourceConnection or -c <connection>           connection string or details for the source
        --sourceFilter or -f <filter>                   filter string to apply to the source
        --sourcePreviousSpanMinutes or -p <minutes>     previous span in minutes to check from the source
        --sourceTakeLimit or -l <count>                 max number of items to take from the source
        --sourceAppOrResourceId or -a <id>              app or resource ID of the source 
        --primaryThresholdValue or -v <value>           primary threshold value to check in the condition
        --secondaryThresholdValue or -u <value>         secondary threshold value to check in the condition (usually used for sampling)
        --terminateWhenConditionMet or -t <bool>        true/false flag to terminate on condition
        --shouldFailOnRunDurationExceeded or -e <bool>  true/false flag to fail on runtime duration exceeded
        --cooldownWhenConditionMetMinutes or -w <c>     cooldown in minutes when the condition is met 
        --sinkType or -n <type>                         type of outbound sink for the monitoring results
        --sinkConnection or -o <connection>             connection string or details for the sink

### Peckr Types
- `azwdlogs_errscnt_upperbound`                    Azure WAD Logs Error Count Upper Threshold
- `azwdperf_instavg_upperbound`                    Azure WAD Performace Counters Instance Average Aggregated Upper Threshold
- `azailogs_slot_traffic`                          Azure AppInsights Slot Traffic Data Peckr
- `azailogs_slot_role_cpu`                         Azure AppInsights CPU Threshold Peckr
- `azailogs_slot_response_times`                   Azure AppInsights Response Times Threshold Peckr
- Stay tuned, more coming soon!

### Quick and easy
#### WAD Logs 
```bash
peckr -m azwdlogs_errscnt_upperbound -c "DefaultEndpointsProtocol=https;AccountName=fooapistor;AccountKey=xxxxxxxxxxxxxxxxxxxx==" -f "EventId ne 1337'"
```

#### WAD Performance Counters
```bash
peckr -m azwdperf_instavg_upperbound -l 1200 -v 75 -c "DefaultEndpointsProtocol=https;AccountName=fooapistor;AccountKey=xxxxxxxxxxxxxxxxxxxx==" -f "Counter eq 'Processor\% Processor Time'" 
```

#### AppInsights Slot Traffic Threshold Peckr
```bash
peckr -m azailogs_slot_traffic -l 15 -p 30 -d 30 -i 60000 -v 1100 -u 80 -c <appinsights-api-key> -t true -a <appinsights-resource-id> -e true -f "requests | extend deploymentId = tostring(customDimensions.DeploymentId)| where deploymentId == \"<slot-deployment-id>\"| summarize AverageRateRequestsPerSecond=sum(itemCount)/10 by deploymentId, bin(timestamp, 10s)" 
```

#### AppInsights CPU Performance Threshold Peckr
```bash
peckr -m azailogs_slot_role_cpu -l 0 -p 60 -d 5 -i 60000 -v 50 -c <appinsights-api-key> -t true -a <appinsights-resource-id> -e false -f "performanceCounters | where counter endswith \"Processor Time Normalized\" | extend deploymentId = tostring(customDimensions.DeploymentId) | where deploymentId == \"<slot-deployment-id>\" | summarize max(value) by bin(timestamp, 2h), cloud_RoleInstance, deploymentId" 
```

#### Azure AppInsights Response Times Threshold Peckr
```bash
peckr -m azailogs_slot_response_times -l 0 -p 60 -d 5 -i 60000 -v 300 -c <appinsights-api-key> -t true -a <appinsights-resource-id> -e false -f "requests | where name !startswith \"GET /diagnostics\" | extend deploymentId = tostring(customDimensions.DeploymentId)  | where deploymentId == \"<slot-deployment-id>\" | extend ep = split(tolower(name), \"/\") | extend lastItemIdx = array_length(ep) - 1 | extend lastSegment = tostring(ep[lastItemIdx]) | extend removeLastSegment = isempty(lastSegment) or isnotnull(toint(lastSegment)) | extend sliceIdx = case(removeLastSegment, lastItemIdx - 1, lastItemIdx) | extend endpoint = strcat_array(array_slice(ep, 1,sliceIdx), \"/\") | summarize percentileResponeTimeMs=percentile(duration, 99) by bin(timestamp, 2h), endpoint, deploymentId" 
```

### Slack Webhook Sink

#### WAD Logs
```bash
peckr --monitorType azwdlogs_errscnt_upperbound --sourceTakeLimit 100 --sourceConnection "DefaultEndpointsProtocol=https;AccountName=fooapistor;AccountKey=xxxxxxxxxxxxxxxxxxxx==" --sourceAppOrResourceId "Foo API" --sourceFilter "EventId ne 1337'" --sinkType slackwebhook --sinkConnection "https://hooks.slack.com/services/zzzzz/yyyyyy/xxxxxx"
```

#### WAD Performance Counters
```bash
peckr --monitorType azwdperf_instavg_upperbound --takeLimit 1000 --previousSpanMinutes 5 --expectedRunDurationMinutes 15 --pollingDelayMilliseconds 10000 --primaryThresholdValue 20 --sourceConnection "DefaultEndpointsProtocol=https;AccountName=fooapistor;AccountKey=xxxxxxxxxxxxxxxxxxxx==" --appOrResourceId "Foo API" --sourceFilter "Counter eq 'Processor\% Processor Time'" --sinkType slackwebhook --sinkConnection "https://hooks.slack.com/services/zzzzz/yyyyyy/xxxxxx"
```

## Development Guide
The [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) is required to build and debug the solution on your local development machine. 
For contributions to the project please see our [CONTRIBUTING.md](./CONTRIBUTING.md) document for a guide on submitting new features or fixes.

### Code Architecture
#### Program
-   args:string[] ￫ DerivePeckrSettings ￫ **PeckrSettings**\
    `Validate configuration and bind to domain settings type`
    -   string[] ￫ PeckrConfiguration ￫ PeckrSettings
-   **PeckrSettings** ￫ **PeckrFactory**.GetPeckr ￫ **IConsolePeckr**\
    `GetPeckr takes Settings and returns PollingConsolePeckr<IReadOnlyCollection<LogEntry>> or PollingConsolePeckr<IReadOnlyCollection<Metric>>`
-   **PeckrSettings** ￫ **IConsolePeckr**.PeckrAsync ￫ **ConsoleExitCode**\
    `PollingConsolePeckr.PeckrAsync takes Settings and returns Success or UnknownError codes`
    -   **PeckrSettings** ￫ **IPeckResultPoller<T>**.PollAsync ￫ IAsyncEnumerable<**IPeckResult<T>**>\
        `T is IReadOnlyCollection<LogEntry> | IReadOnlyCollection<Metric>`
        -   (start:DateTimeOffset,end:DateTimeOffset,takeLimit:int,customFilter:string,allowFailures:bool) ￫ **IPeckrDataRetriever<T>**.GetAsync ￫ T
    -   **(IPeckResult<T>**,**PeckrSettings)** ￫ **IPeckResultSink<T>**.PushPeckResultAsync ￫ Task\
        `PushPeckResultAsync takes the result and settings to push to a derived IPeckResultSink`