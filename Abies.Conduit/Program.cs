using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Abies;
using Abies.Conduit.Main;
using Abies.Conduit;

var conduit = Browser.Application<Application, Arguments, Model>();

// Hook into the program to handle API commands during the update cycle
// API commands are handled via ApiCommandHandling extensions

await Runtime.Run(conduit, new Arguments());