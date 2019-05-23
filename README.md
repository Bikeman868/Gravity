# Gravity

A reverse proxy and load balancer written with C# and running on OWIN

## Status

This has reached MVP now and has received some testing. If you are interested in
using this code please beta test and let me know what you find.

## Features

This is an OWIN based service. The source code as-is will run on IIS but you can
easily customize it to run on Apache, self host (.exe) etc as supported by OWIN.

You can configure a graph of nodes that define how requests are processed and link
these nodes together in arbirtary ways. Any node can have its 'disabled' property
set to temporarily turn it off thsi can be useful for example when you want to
remove a server from the load balancer for maintenance or put up a maintenance mssage
when the whole site is down for maintenance.

The node types are:

### Listener Node

Defines an IP address and port number to process requests for. Also support wildcard
that matches all IP addresses and/or all ports. If running on IIS these IP addresses
and ports must be configured in the IIS bindings.

Each listener measures the request rate and average request processing time.

### Server Node

Defines a server to send requests to. You can specify an IP address or DNS name, and
when a DNS name is specified it can resolve to multiple IP addresses. The server health
checks all IP addresses and distributes requests across all the healthy ones. This means
that you can bind your web server to multiple IP addresses on different networks and if
one network fails the traffic will all be routed to the one that still works.

Each server node measures the request rate and average request processing time for
each IP address.

If all IP addresses for the server are unhealthy then the server is taken offline.
When the server is offline all upstream nodes are also taken offline so that for
example if you put a round robin node in front of multiple servers then any failing
servers will not be sent requests by the round robin.

### Router Node

Matches requests and directs traffic to different paths through the load balancer.
For example you can route requests to different pools of servers based on the host
header, path, query string etc. You can also route GET requests to a larger pool
of servers than PUT, POST and DELETE requests for example.

### Transform Node

Applies URL Rewrite module rules to a request. See the readme file in my UrlRewrite.Net 
repo for a detailed definition of the rule syntax. Rules can be applied to the incomming
request, the outbound response or both. The rules can be embedded directly into the
load balancer configuration or supplied in an separate file.

### Response Node

Returns a fixed rersponse. This is useful for serving static content, returning 404,
putting up a maintenance message when your website is down for updates etc.

### CORS Node

Implements the logic required to support CORS requests - for example if you have
web services in sub-domains that need to be called from your main site domain.

### Internal Node

Any requests routed to this node will be passed to the rest of the OWIN pipeline
and handled within the load balancer. You can use this to expose the UI that the
load balancer contains for checking the configuration and monitoring traffic flow
through the load balancer.

### Round Robin Node

These nodes are configured with a list of output nodes and forward requests in a
round robin fashion. Any outputs connected to a downstream that is offline will be
not have requests sent to them so that we are not sending requests to dead servers.

### Sticky Session Node

These nodes look for a session cookie in the request and send requests with a
specific session IO always to the same server. When a new session ID is encountered
the request is send to the server with the least number of active connections and
the session cookie that is set by the server is captured in the response so that
this client will be routed to the same server again on the next request.

### Least Connections Node

These nodes have a list of nodes to send requests to next and always send requests
to the output that has the least number of active connections.

