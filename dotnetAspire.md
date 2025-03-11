# Running the UDAP Dotnet Tutorial with .NET Aspire

To run the UDAP Dotnet Tutorial project using .NET Aspire, follow these steps:

## Prerequisites

Ensure you have the following installed on your machine:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Aspire CLI](https://github.com/dotnet/aspire)


### Asp.NET Development Certificate for TLS

Extract the existing localhost dev cert certificate from the user keystore.  The certificate friendly name is ```ASP.NET Core HTTPS development certificate```.
During extraction include the private key and set the password to ```password```.
Save the certificate in the ./CertificateStore folder.  This will allow the aspire.AppHost project to mount the certificate into the udaped container.

### EMR Direct UDAP Client Certificate

If you have a EMR Direct issued client certificate you would like to add to the UdapEd Container can pick it up
then save the certificate in ${USERPROFILE}/.aspnet/https.  This will allow docker-compose.yml to mount the certificate into the container.

```json
{
  "sampleKeyC": "yourpassword"
}
```

Second you will need to supply a password to open the P12 file via the SampleC environment variable.  
Add the following sampleKeyC property to the ```aspire.AppHost``` project in the ```appsettings.json``` file.
If you are familiar with secrets.json via the UserSecretsId link in the ```aspire.AppHost``` project then add the password there.

## Steps

1. **Clone the Repository**

   Clone the `udap-dotnet` repository to your local machine if you haven't already:
2. **Build the Solution**

   From the root of the repository, run the following command to build the solution:
   ```dotnet build```

3. **Run the Projects with .NET Aspire**

   Use the Aspire CLI to run the projects. Run the following command in the root directory of the repository:
This command will start all the projects defined in the Aspire configuration.

4. **Access the Services**

   Once the projects are up and running, you can access the various services using the following URLs:

   - **FHIR Server**: [https://host.docker.internal:7017/fhir/r4](https://host.docker.internal:7017/fhir/r4)
   - **UDAP Auth Server**: [https://host.docker.internal:5102](https://host.docker.internal:5102)
   - **UDAP IDP Server**: [https://host.docker.internal:5202](https://host.docker.internal:5202)

## Additional Notes

- Ensure that the ports specified in the Aspire configuration for each project do not conflict with other services running on your machine.
- If you need to regenerate certificates during the tutorial, follow the instructions in the [README.md](README.md) file.  
  You may also need to delete the existing database and restart the services.


