Add-on for [LightPerms](https://torchapi.com/plugins/view/?guid=5c3f35b3-ac9d-486f-8559-f931536c6700) plugin to have better control over commands permissions.

### Usage

All existing torch commands are indexed as `command.something`, spaces are replaced by `.`

`!help` and `!longhelp` commands are modified to reflect permission requirements.

For example if you want give to players ability to use `!fixship` command, you need to invoke this command `!lp add perm player command.fixship`

Wildcards are also supported, for example command to give to admins ability to use all lp commands (you need to create group before using that command) `!lp add perm admin command.lp.*`