/* CORBAInit.cs
 * 
 * Project: IIOP.NET
 * IIOPChannel
 * 
 * WHEN      RESPONSIBLE
 * 28.01.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
 * 
 * Copyright 2003 Dominic Ullmann
 *
 * Copyright 2003 ELCA Informatique SA
 * Av. de la Harpe 22-24, 1000 Lausanne 13, Switzerland
 * www.elca.ch
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */


using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using Ch.Elca.Iiop;
using Ch.Elca.Iiop.Util;
using omg.org.CosNaming;

namespace Ch.Elca.Iiop.Services {

    /// <summary>
    /// provides helper methods to access nameservice and other inital services
    /// </summary>
    public class CorbaInit {

        #region SFields
        
        /// <summary>the singleton init</summary>
        private static CorbaInit s_init = new CorbaInit();

        #endregion SFields
        #region IFields

        /// <summary>known references to INIT-services</summary>
        /// <remarks>caching references to INIT-services, they remain the same</remarks>
        private Hashtable m_initalServices = new Hashtable();

        #endregion IFields
        #region IConstructors
        
        private CorbaInit() {
        }

        #endregion IConstructors
        #region SMethods

        public static CorbaInit GetInit() {
            return s_init;
        }
        
        #endregion SMethods
        #region IMethods
        	
        public NamingContext GetNameService(string host, int port, GiopVersion version) {
			string nsKey = String.Format("NameService:{0}:{1}:{2}.{3}", host, port, 
                                         version.Major, version.Minor);
	
			lock(m_initalServices.SyncRoot) {
				NamingContext result = null;
				if (!m_initalServices.ContainsKey(nsKey)) {
				
				    string objectURI = IiopUrlUtil.GetObjUriForObjectInfo(InitialCOSNamingContextImpl.s_initalnamingObjKey,
                                                                  version);
                    string url = IiopUrlUtil.GetUrl(host, port, objectURI);
                    result = (NamingContext)RemotingServices.Connect(typeof(NamingContext), url);
                    m_initalServices.Add(nsKey, result);
                } else {
                	result = (NamingContext)m_initalServices[nsKey];
                }
                return result;
            }
        }

        public NamingContext GetNameService(string host, int port) {
            return GetNameService(host, port, new GiopVersion(1, 0));
        }
        
        #endregion IMethods
        
    }

    /// <summary>
    /// provides access to the init-services. 
    ///	The concept of the INIT service is specific to RMI/IIOP.
    /// </summary>
    public class RmiIiopInit {

        #region IFields

        /// <summary>known references to INIT-services</summary>
        /// <remarks>caching references to INIT-services, they remain the same</remarks>
        private Hashtable m_initalServices = new Hashtable();
        
		/// <summary>the INIT service object</summary>
		private CORBAInitService m_initService;

        #endregion IFields
        #region IConstructors
        
        public RmiIiopInit(string host, int port) {
            m_initService = GetInitService(host, port);
        }

        #endregion IConstructors
        #region IMethods
        
        /// <summary>get a reference to the CORBA naming service from the init service</summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns>a reference to the naming service</returns>
        /// <remarks>This method is only useful, for communication with the RMI/IIOP SUN JDK ORB</remarks>
        public omg.org.CosNaming.NamingContext GetNameService() {
            return (omg.org.CosNaming.NamingContext)m_initService._get(CORBAInitServiceImpl.NAMESERVICE_NAME);            
        }
        
        /// <summary>
        /// gets a reference to the initial service with the name name, e.g. TradingService or NameServiceServerRoot.
        /// </summary>
        public MarshalByRefObject GetService(string name) {
            return m_initService._get(name);
        }
        
        /// <summary>gets a reference to the init service running at the specified host at the specified port</summary>
        private CORBAInitService GetInitService(string host, int port) {
            lock(m_initalServices.SyncRoot) {
                CORBAInitService initService = null;
                string key = host + ":" + port;
                if (!m_initalServices.ContainsKey(key)) {
                    string objectURI = GetInitServiceUri();
                    string url = IiopUrlUtil.GetUrl(host, port, objectURI);
                    initService = (CORBAInitService)RemotingServices.Connect(typeof(CORBAInitService), url);
                    m_initalServices.Add(key, initService);
                } else {
                    initService = (CORBAInitService) m_initalServices[key];
                }
                return initService;
            }
        }

        /// <summary>
        /// get the obj-uri for the CORBA init service
        /// </summary>
        /// <returns></returns>
        public static string GetInitServiceUri() {
            return IiopUrlUtil.GetObjUriForObjectInfo(CORBAInitServiceImpl.s_corbaObjKey,
                                                      new GiopVersion(1, 0));
        }

        #endregion IMethods

    }
}
