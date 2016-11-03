# AgcubioServer

The Agcubio Server is a MVC application used to play the game Agcubio. Agcubio functions similarly to Agario where the player (A cube) 
wanders around the map and eats food pellets. The player then grows depending on how many food pellets he has eaten. Depending on his size,
he is also able to consume other players by moving over them. The bigger the player, the slower he moves. Pressing the spacebar button 
allows a user to split into smaller cube and thus move faster or 'shoot' himself to another player and consume them.

Viruses also exist on the map which will split a player if it is touched. This allows other players to take advantage of the biggest player
and consume his smaller pieces. 

The Agcubio Server 'monitors' and takes care of what is happening on the map. The server sends the current state of the game information
to the player and vice versa. 
