/* Generated By:JJTree: Do not edit this line. ASTliteral.cs */

/* ASTliteral.cs
 * 
 * Project: IIOP.NET
 * IDLToCLSCompiler
 * 
 * WHEN      RESPONSIBLE
 * 14.02.03  Dominic Ullmann (DUL), dominic.ullmann -at- elca.ch
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

namespace parser {

public class ASTliteral : SimpleNode {

    #region IFields

    private Object m_litVal;
    
    #endregion IFields
    #region IConstructors

    public ASTliteral(int id) : base(id) {
    }

    public ASTliteral(IDLParser p, int id) : base(p, id) {
    }

    #endregion IConstructors
    #region IMethods

    /** Accept the visitor. **/
    public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
        return visitor.visit(this, data);
    }

    public void setLitVal(Object litVal) {
        m_litVal = litVal;
    }
    public Object getLitVal() {
        return m_litVal;
    }
    
    #endregion IMethods

}

}