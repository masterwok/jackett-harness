using System;
using Android.Runtime;
using Com.Masterwok.Xamarininterface;

namespace Library
{
    [Register("com/masterwok/xamarindependency/HelloXamarinService")]
    public class HelloXamarinService : Java.Lang.Object, IHelloService
    {
        public string CreateHello()
        {
            return "Hello Xamarin!";
        }
    }
}
