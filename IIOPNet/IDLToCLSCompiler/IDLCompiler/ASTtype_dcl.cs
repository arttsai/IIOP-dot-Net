/* Generated By:JJTree: Do not edit this line. ASTtype_dcl.cs */

using System;

namespace parser {

public class ASTtype_dcl : SimpleNode {
  public ASTtype_dcl(int id) : base(id) {
  }

  public ASTtype_dcl(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}

