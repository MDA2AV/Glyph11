using GenHTTP.Engine.Internal;
using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Layouting;

var functionalService = Inline.Create()
    .Get("", () => "Hello from GenHTTP Minimal API");

var api = Layout.Create()
    .Add("", functionalService);

await Host.Create()
    .Handler(api)
    .Development()
    .Console()
    .RunAsync();