## Build migrations

``dotnet ef migrations add PersistedGrant -c PersistedGrantDbContext -o IdentityServer/Migrations/PersistedGrantDb  --namespace=IdentityServer.Migrations.PersistedGrantDb``

``dotnet ef migrations add Configuration -c ConfigurationDbContext -o IdentityServer/Migrations/ConfigurationDb  --namespace=IdentityServer.Migrations.ConfigurationDb``

``dotnet ef migrations add UdapGrant -c UdapDbContext -o IdentityServer/Migrations/UdapDb  --namespace=IdentityServer.Migrations.UdapDb``