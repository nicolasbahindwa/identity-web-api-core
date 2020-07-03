namespace identity_web_api_core.Services
{
    internal class SymetricSecurityKey
    {
        private byte[] vs;

        public SymetricSecurityKey(byte[] vs)
        {
            this.vs = vs;
        }
    }
}