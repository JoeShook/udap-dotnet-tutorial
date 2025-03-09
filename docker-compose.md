# Running the UDAP Dotnet Tutorial with Docker Compose

To run the UDAP Dotnet Tutorial project using Docker Compose, follow these steps:

## Prerequisites

Ensure you have the following installed on your machine:
- [Docker](https://www.docker.com/get-started)
- [Docker Compose](https://docs.docker.com/compose/install/)

### Asp.NET Development Certificate

Extract the existing localhost dev cert certificate from the user keystore.  The certificate friendly name is ```ASP.NET Core HTTPS development certificate```.
During extraction include the private key and set the password to ```password```.
Save the certificate in ${USERPROFILE}/.aspnet/https.  This will allow docker-compose.override.yml to mount the certificate into the container.


## Steps

1. **Clone the Repository**

   Clone the `udap-dotnet` repository to your local machine if you haven't already:
2. **Build the solution**
  
  From root of repository run the following command:
  ```dotnet build```

3. **Build and Run the Docker Containers**

   From the root of the repository, run the following command:
   ```docker-compose up```

3. **Access the Services**

   Once the containers are up and running, you can access the various services using the following URLs:

   - **FHIR Server**: [https://localhost:7017/fhir/r4](https://localhost:7017/fhir/r4)
   - **UDAP Auth Server**: [https://localhost:5102](https://localhost:5102)
   - **UDAP IDP Server**: [https://localhost:5202](https://localhost:5202)


## Additional Notes

- If you need to regenerate certificates during the tutorial, follow the instructions in the [README.md](README.md) file to delete the existing database and restart the services.
- If you have Visual Studio installed, you can open the project and run the docker-compose.dcproj project to start the services.
