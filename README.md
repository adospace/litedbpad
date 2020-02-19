# LiteDbPad
LinqPad driver for LiteDB

[![Build status](https://ci.appveyor.com/api/projects/status/0ckrd3197ggxcny6?svg=true)](https://ci.appveyor.com/project/adospace/litedbpad)

# LinqPad version 4 and 5 (.NET Full)
LiteDbPad is a Dynamic driver for LinqPad. I've tested it in LinqPad 4.x but should work with version 5 just fine.

To install or update LiteDbPad: 
1) Download LiteDBPad.lpx
2) in LinqPad Add Connection->More Drivers->Browse to LPX file
Once installed, you should be able to select the driver in the Add Connection dialog.

LiteDbPad can open LiteDB 4.0+ files and let you specify some connection string properties like Password etc.

# LinqPad version 6 (.NET Core)
Install directly from LinqPad:

To open LiteDB version 4 databases:
Add new connection->More Drivers->Show All Drivers->**LiteDBPad6**

To open LiteDB version 5+ databases:
Add new connection->More Drivers->Show All Drivers->**LiteDB5Pad6**

Query database with expressions like

```c#
<mycollectionname>.FindAll()
```
