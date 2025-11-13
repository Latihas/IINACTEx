using System.Collections.Generic;
using Triggernometry;
using Triggernometry.Variables;

int a=0;
RealPlugin._plug.sessionvars.Scalar["a"] = new()
{
    Value = "1"
};
RealPlugin._plug.sessionvars.List["a"] = new()
{
    Values = []
};

RealPlugin._plug.sessionvars.Dict["a"] = new()
{
    Values = new Dictionary<string, Variable>()
    {
        
    }
};
RealPlugin._plug.sessionvars.Table["a"] = new()
{
};
