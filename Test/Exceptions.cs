using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    /*
        Encuentro un try:
            No hago nada

        Encuentro un  throw:
            Me fijo dado en que try estoy, cual es el primer catch de ese try. Por el subtipado entraria a la version correcta. Los catch de un un try estan consecutivos.
            Setea var global excepcion al nuevo objeto.
            throw ri, ri tambien se setea con la nueva excepcion.

        Encuentro un catch:
            La instruccion que marca y declara el inicio de un catch pasarlo a un if de subtipado de la variable global exception.
            Al entrar al if, se setea la variable global en null. Antes la variable del catch se setea con la global
            El framework de zoppi le agrega un "terminator" a ese bloque al finally o donde corresponda, por eso no nos preocupamos en agregar un else
     */

    class Exceptions
    {
        public static void exceptions()
        {
            int i = 0;
            try
            {
                try
                {
                    if (i == 0)
                        throw new Exception();
                    else throw new NullReferenceException();
                }
                catch (NullReferenceException ex)
                {
                    i = 10;
                }
                catch (Exception ex)
                {
                    i = 111;
                }

                i = 17;

            }
            catch (NullReferenceException ex)
            {
                i = 1;
            }
            catch (Exception ex)
            {
                i = 1;
            }
            finally
            {
                i = 2;
            }

            i = 5;
        }

        public static void exceptions1()
        {
            int i = 0;
            try
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception ex)
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                i = 1;
            }
        }
    }
}
