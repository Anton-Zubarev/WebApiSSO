# Авторизация WS Federations (ADFS)

Подключение авторизации ADFS к существующему проекту.
Прежде он использовал Windows авторизацию.

Простое повторение кода из https://learn.microsoft.com/ru-ru/aspnet/core/security/authentication/ws-federation?view=aspnetcore-10.0

```` java
services.AddAuthentication()
.AddWsFederation(options =>
{
    // MetadataAddress represents the Active Directory instance used to authenticate users.
    options.MetadataAddress = "https://<ADFS FQDN or AAD tenant>/FederationMetadata/2007-06/FederationMetadata.xml";

    // Wtrealm is the app's identifier in the Active Directory instance.
    // For ADFS, use the relying party's identifier, its WS-Federation Passive protocol URL:
    options.Wtrealm = "https://localhost:44307/";
});
````

не сработало.

Как я понял причину - сертификат скачивается или долго или с ошибкой. Но точного понимания найти не смог.

Тогда я сделал файл сертификата статическим файлом и в коде зполняю из него нужные поля.


