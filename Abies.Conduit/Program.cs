using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Abies;
using Abies.Conduit.Main;
using static Abies.Conduit.Main.Message.Event;

var conduit = Browser.Application<Application, Arguments, Model>();

await Runtime.Run(conduit, new Arguments());