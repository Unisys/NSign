# Runtime Selection for Signing Keys

This example shows how the key used for signing can be chosen at runtime, based
on the authenticated user. From this you can extrapolate the following too:

* How to select an algorithm (including the key material) at runtime
* How to select a signature verifier (including the key material) at runtime

## How to run

Before you can run this example, you'll need to change the settings in `appsettings.json`
to have an actual working endpoint to send outbound requests to:

```json
{
  // ...
  "Outbound": {
    "TargetEndpoint": "<your-http-or-https-endpoint-here>"
  }
}
```

For instance, you could get your own temporary web request inspector endpoint at
[webhook.site](https://webhook.site/).

Once the web server is up and running, you can navigate to
[https://localhost:7002/api/test](https://localhost:7002/api/test) to have the
server send a signed `POST` request to the above configured `TargetEndpoint`,
using a key based on the authenticated user.

You can pass *any* username and password, they are *not* checked. Obviously, you
wouldn't do that in a production environment ;-)
