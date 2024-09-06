# Contribute
Sveve does a great job at keeping their API stable and has done so for many years.  
I therefore do not expect to change this client in any significant way once released.  

That said, I welcome contributions and discussions!  
To build and test locally, clone this repository and make a new file:  
`Sveve.Tests/appsettings.local.json`
and write the following:
```json
{
    "Sveve": {
        "Username": "<your Sveve username>",
        "Password": "<your Sveve password>"
    }
}
```

For obvious reasons, `Sveve.Tests/appsettings.local.json` is ignored by GitHub.