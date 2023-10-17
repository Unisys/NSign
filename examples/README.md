# NSign Examples

Here you can find some working examples of NSign for your reference on how to
use NSign.

Examples include:

* [Publisher](Publisher/):
  A simple console app that sends signed HTTP request messages to an endpoint.
* [Subscriber](Subscriber/):
  A simple web app that receives signed HTTP request messages, verifies the
  signature and, if everything is ok, responds with a HTTP `200 OK` message.
* [RuntimeKeySelection](RuntimeKeySelection/):
  A web app that signs outgoing requests (made in response to incoming user
  requests) with a key that is derived from the authenticated user.
