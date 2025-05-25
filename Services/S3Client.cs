using Amazon.CognitoIdentity.Model;
using Amazon.CognitoIdentity;
using Amazon.Runtime;
using Amazon.S3;

namespace SwiftSpecBuild.Services
{
    public class S3Client
    {
        public async Task<AmazonS3Client> CreateS3ClientFromTokenAsync(string idToken)
        {
            var identityPoolId = "eu-west-1:c8709566-d375-4514-bbe3-ba7712662888";

           
            var cognitoIdentity = new AmazonCognitoIdentityClient();

            // Get Identity ID
            var getIdRequest = new GetIdRequest
            {
                IdentityPoolId = identityPoolId,
                Logins = new Dictionary<string, string>
        {
            { "cognito-idp.eu-west-1.amazonaws.com/eu-west-1_bk8cS7E2C", idToken }
        }
            };

            var getIdResponse = await cognitoIdentity.GetIdAsync(getIdRequest);

            // Get temporary credentials
            var getCredsRequest = new GetCredentialsForIdentityRequest
            {
                IdentityId = getIdResponse.IdentityId,
                Logins = getIdRequest.Logins
            };

            var credsResponse = await cognitoIdentity.GetCredentialsForIdentityAsync(getCredsRequest);
            var credentials = credsResponse.Credentials;

            // Return S3 client using these temporary credentials
            var tempCreds = new SessionAWSCredentials(
                credentials.AccessKeyId,
                credentials.SecretKey,
                credentials.SessionToken
            );

            return new AmazonS3Client(tempCreds, Amazon.RegionEndpoint.EUWest1);

        }
    }
}
