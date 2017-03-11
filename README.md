# litedbpad
LinqPad driver for LiteDB

LiteDbPad is a Dynamic driver for LinqPad. I've tested it in LinqPad 4.x but should work with version 5 just fine.

To install LiteDbPad: 
1) Download LiteDBPad.lpx
2) in LinqPad Add Connection->More Drivers->Browse to LPX file
Once installed, you should be able to select the driver in the Add Connection dialog.

LiteDbPad can open LiteDB 3.0 files and let you specify some connection string properties like Password etc.

Query database with expressions like

```c#
mycollectionname.FindAll()
```
