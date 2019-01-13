using LibRpwsDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibRpws2.QuestionArgs
{
    public class ValidateAccessTokenArgs
    {
        public string token;
    }

    public class ValidateAccessTokenReplyArgs
    {
        public bool ok;
        public E_RPWS_Token token;
    }
}
