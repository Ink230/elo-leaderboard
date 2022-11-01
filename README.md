## Websocket ELO Leaderboard Manager
This simple project brings together a standalone Redis server, NetCoreServer and WebSockets in .NET through a TLS NGINX reverse proxy so that a user may add users, update scores and add new game completions to an ELO leaderboard. ELO is calculated for up to any number of users involved in the match (1vs1, 1vs1vs1vs1, 4vs4...etc).

Note this code is not maintained and is a simple proof of concept.

## Setup
### .NET
<ul>
<li>Configure secrets.json in whichever method you prefer and insert it in the WsServer redis setup property</li>
<li>Configure the ELO system to use the match size and number of players as desired</li>
<li>Configure the ELO system's commands as noted from the example 'catan' command</li>
</ul>


### NGINX
<ul>
<li>Configure NGNIX as a reverse proxy and direct to the .NET instance</li>
<li>Proxy pass the necessary headers you desire</li>
</ul>


### Redis
<ul>
<li>Setup a Redis server or connect a serverless provider</li>
<li>Ensure the .NET Redis package is configured correctly</li>
<li>Seed initial users using the Redis cli or extend the .NET code for support</li>
</ul>

### Frontend Client
<ul>
<li>Basic HTML, JS and knowledge of connecting over websockets with TLS/wss</li>
<li>Simple form to submit text commands is sufficient</li>
</ul>
