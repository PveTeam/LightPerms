Light plugin to add permission system (like minecraft's PermissionEx or LuckPerms) to your server.


In this plugins all permissions can be wildcards and belongs to groups

By default plugin creates and assigns `player` group to all players automatically (currently unchangeable)

### Commands

+ `!lp get groups` - Returns list of all groups
+ `!lp add group <group name>` - Creates a new group
+ `!lp del groups <group name>` - Deletes the group
+ `!lp get perms <group name>` - Returns list of permissions in the group
+ `!lp add perm <group name> <permission>` - Adds a new permission to the group
+ `!lp del perm <group name> <permission>` - Deletes a permission from the group
+ `!lp has perm <permission> <client id>` - Returns if player has a permission (client id can be removed to use the sender id)
+ `!lp assign group <group name> <client id>` - Assigns a group to the player (client id can be removed to use the sender id)

### Groups based on discord roles

To automatically assign a group to players if they have a specific role in discord server use [LightPerms.Discord](https://torchapi.com/plugins/view/?guid=d53cf5e6-27ea-491b-9579-8506d93f184b) plugin

### Support in plugins

+ [Kits](https://torchapi.com/plugins/view/?guid=d095391d-b5ec-43a9-8ba4-6c4909227e6e)