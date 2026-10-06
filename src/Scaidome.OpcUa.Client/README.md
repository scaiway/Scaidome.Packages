
# Configuration

The client reads `Scaidome.OpcUa.Client.Config.xml`, which the package copies to the application's output folder. It is read
from there whatever the current directory is, so an application started elsewhere, by `dotnet run` or as a Windows service,
finds it. When the output folder has none, the OPC UA SDK looks in the current directory.

# Opc UA Security
There's different kind of security. 
- The server authenticates and accepts a client using the clients certificate
- The client authenticates and accepts a server using the servers certificate
- Ensuring communication is not tampered with using certificates
- Encrypting communication to avoid man-in-the-middle snooping


# Opc UA Certificates

Opc UA clients and servers both have their own certificates. The trust must be established both ways.

The Scaidome client will try to connect to the server endpoint with the "best" security using CoreClientUtils.SelectEndpoint(()
The first time the client will reject the server certificate and put it in rejected/certs.
Move the rejected server certificate to trusted/cert, and restart the client.
The same procedure must be done for the server, which also will reject the client certificate the first time a session is being connected.

Known public opc ua servers:
https://github.com/digitalpetri/opc-ua-demo-server

# PKI Certificate Main Folders
- CommonApplicationData (%PROGRAMDATA% in explorer -> C:\ProgramData)
- LocalApplicationData (%LOCALAPPDATA% in explorer -> C:\Users\<USERNAME>\AppData\Local)

# PKI Certificate Store Structure

- **/pki**
  - **/own**
    - `certs/` (server public certificate)
    - `private/` (server private key)
  - **/rejected**
    - `certs/` (rejected client certificates)
  - **/trusted**
    - `certs/` (trusted client/CA certificates)
    - `crl/` (CRLs for trusted certificates)
  - **/issuer**
    - `certs/` (CA certificates for validation)
    - `crl/` (CRLs for issuer CAs)
  - **/trustedUser**
    - `certs/` (trusted user/user CA certificates)
    - `crl/` (CRLs for user certificates)
  - **/issuerUser**
    - `certs/` (user CA certificates)
    - `crl/` (CRLs for user CA validation)