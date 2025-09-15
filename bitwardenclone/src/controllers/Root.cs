namespace bitwardenclone.src.controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
public class Root: Controller {

    [HttpGet("ping")]
    public string Ping(string? echoThis) {
        if (echoThis is string echoThisForReal) {
            return $"Pong! You said {echoThisForReal}!";
        }

        return "Pong!";
    }
}
