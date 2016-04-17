# flexHttpd
A lightweight http server for Windows IoT (and anything else).

`PM> Install-Package FlexHttpd` 

## Prepare
Open your `Package.appxmanifest` and make sure your application has `Internet (Client & Server)` and / or `Private Networks (Client & Server)` capabilities (depending on your needs).

## Example

```c#
private FlexServer _httpd;

private void StartHttpd()
{
    _httpd = new FlexServer();

    _httpd.Get["/device/on"] = async (request) =>
    {
        // do whatever you need to do here
        // ...
        // await _magicDevice.TurnOn();
        // ...
        return new FlexResponse(FlexHttpStatus.Ok);
    };

    Task.Factory.StartNew(async () => await _httpd.Start(1024));
}
```

Enjoy! :)