namespace GenHTTP.Api.Draft.Protocol;

public enum RequestMethod
{
    Get,
    Head,
    Post,
    Put,
    Delete,
    Connect,
    Options,
    Trace,
    Patch,

    // if it cannot be parsed into one of the ones above
    Other

}
