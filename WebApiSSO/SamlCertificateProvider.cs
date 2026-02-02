namespace WebApiSSO;

using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;

public class SamlCertificateProvider
{
    public X509Certificate2 Certificate { get; }
    public string ValidIssuer { get; }
    /// <summary>
    /// В корне должен быть FederationMetadata.xml
    /// (качать из https://sso.carcade.com/FederationMetadata/2007-06/FederationMetadata.xml)
    /// </summary>
    /// <exception cref="Exception"></exception>
    public SamlCertificateProvider()
    {
        string baseDir = AppContext.BaseDirectory;
        string xmlPath = Path.Combine(baseDir, "FederationMetadata.xml");

        XDocument xmlDoc = XDocument.Load(xmlPath);

        XNamespace md = "urn:oasis:names:tc:SAML:2.0:metadata";
        XNamespace ds = "http://www.w3.org/2000/09/xmldsig#";

        var certificateBase64 = xmlDoc.Root?
            .Element(ds + "Signature")?
            .Element(ds + "KeyInfo")?
            .Element(ds + "X509Data")?
            .Element(ds + "X509Certificate")?.Value ?? throw new ArgumentNullException("X509Certificate");

        var validIssuer = xmlDoc.Root?.Attribute("entityID")?.Value;

        byte[] certBuffer = Convert.FromBase64String(certificateBase64);
        var certificate = new X509Certificate2(certBuffer);

        Certificate = certificate;
        ValidIssuer = validIssuer ?? throw new ArgumentNullException(nameof(validIssuer));
    }

    public SamlCertificateProvider(X509Certificate2 certificate, string validIssuer)
    {
        Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        ValidIssuer = validIssuer ?? throw new ArgumentNullException(nameof(validIssuer));
    }
}