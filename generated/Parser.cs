/*----------------------------------------------------------------------
Compiler Generator Coco/R,
Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
extended by M. Loeberbauer & A. Woess, Univ. of Linz
with improvements by Pat Terry, Rhodes University

This program is free software; you can redistribute it and/or modify it 
under the terms of the GNU General Public License as published by the 
Free Software Foundation; either version 2, or (at your option) any 
later version.

This program is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License 
for more details.

You should have received a copy of the GNU General Public License along 
with this program; if not, write to the Free Software Foundation, Inc., 
59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.

As an exception, it is allowed to write an extension of Coco/R that is
used as a plugin in non-free software.

If not otherwise stated, any source code generated by Coco/R (other than 
Coco/R itself) does not fall under the GNU General Public License.
-----------------------------------------------------------------------*/

using System;

namespace Tastier {



public class Parser {
	public const int _EOF = 0;
	public const int _number = 1;
	public const int _ident = 2;
	public const int _string = 3;
	public const int maxT = 49;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

const int // object kinds
      var = 0, proc = 1, constant = 2, scope = 3,  str = 4;

   const int // types
      undef = 0, integer = 1, boolean = 2;

   public SymbolTable tab;
   public CodeGenerator gen;

/*-------------------------------------------------------------------------------------------*/



	public Parser(Scanner scanner) {
		this.scanner = scanner;
		errors = new Errors();
	}

	void SynErr (int n) {
		if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
		errDist = 0;
	}
	
	void Get () {
		for (;;) {
			t = la;
			la = scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}


	bool WeakSeparator(int n, int syFol, int repFol) {
		int kind = la.kind;
		if (kind == n) {Get(); return true;}
		else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}

	
	void AddOp(out Op op) {
		op = Op.ADD; 
		if (la.kind == 4) {
			Get();
		} else if (la.kind == 5) {
			Get();
			op = Op.SUB; 
		} else SynErr(50);
	}

	void Expr(out int reg,        // load value of Expr into register
out int type) {
		int typeR, regR; Op op; 
		SimExpr(out reg,
out type);
		if (StartOf(1)) {
			RelOp(out op);
			SimExpr(out regR,
out typeR);
			if (type == typeR) {
			  type = boolean;
			  gen.RelOp(op, reg, regR);
			}
			else SemErr("incompatible types");
			
			if (la.kind == 25) {
				TernaryOp(out reg, out type);
			}
		}
		gen.ClearRegisters(); 
	}

	void SimExpr(out int reg,     //load value of SimExpr into register
out int type) {
		int typeR, regR; Op op; 
		Term(out reg,
out type);
		while (la.kind == 4 || la.kind == 5) {
			AddOp(out op);
			Term(out regR,
out typeR);
			if (type == integer && typeR == integer)
			  gen.AddOp(op, reg, regR);
			else SemErr("integer type expected");
			
		}
	}

	void RelOp(out Op op) {
		op = Op.EQU; 
		switch (la.kind) {
		case 19: {
			Get();
			break;
		}
		case 20: {
			Get();
			op = Op.LSS; 
			break;
		}
		case 21: {
			Get();
			op = Op.GTR; 
			break;
		}
		case 22: {
			Get();
			op = Op.NEQ; 
			break;
		}
		case 23: {
			Get();
			op = Op.LEQ; 
			break;
		}
		case 24: {
			Get();
			op = Op.GEQ; 
			break;
		}
		default: SynErr(51); break;
		}
	}

	void TernaryOp(out int reg,  //Loads value of ? x : y int register based on last RelOp
out int type) {
		int l0, l1; 
		Expect(25);
		gen.ClearRegisters();
		l0 = gen.NewLabel();
		l1 = gen.NewLabel();
		
		gen.BranchFalse(l0);
		Expr(out reg, out type);
		gen.Branch(l1); 
		Expect(26);
		gen.Label(l0); 
		Expr(out reg, out type);
		gen.Label(l1); 
	}

	void Primary(out int reg,     // load Primary into register
out int type) {
		int n; Obj obj; string name; 
		type = undef;
		reg = gen.GetRegister();
		
		switch (la.kind) {
		case 2: {
			Ident(out name);
			obj = tab.Find(name); type = obj.type;
			
			if (la.kind == 6) {
				Get();
				Ident(out name);
				obj = tab.Find(obj.name+name);
				
			}
			if (obj.kind == var || obj.kind == constant) {
			  if (obj.level == 0)
			     gen.LoadGlobal(reg, obj.adr, name);
			  else
			     gen.LoadLocal(reg, tab.curLevel-obj.level, obj.adr, name);
			  if (type == boolean)
			  // reset Z flag in CPSR
			     gen.ResetZ(reg);
			}
			else SemErr("variable/constant expected");
			
			break;
		}
		case 1: {
			Get();
			type = integer;
			n = Convert.ToInt32(t.val);
			gen.LoadConstant(reg, n);
			
			break;
		}
		case 5: {
			Get();
			Primary(out reg,
out type);
			if (type == integer)
			  gen.NegateValue(reg);
			else SemErr("integer type expected");
			
			break;
		}
		case 7: {
			Get();
			type = boolean;
			gen.LoadTrue(reg);
			
			break;
		}
		case 8: {
			Get();
			type = boolean;
			gen.LoadFalse(reg);
			
			break;
		}
		case 9: {
			Get();
			Expr(out reg,
out type);
			Expect(10);
			break;
		}
		default: SynErr(52); break;
		}
	}

	void Ident(out string name) {
		Expect(2);
		name = t.val; 
	}

	void String(out string text) {
		Expect(3);
		text = t.val; 
	}

	void MulOp(out Op op) {
		op = Op.MUL; 
		if (la.kind == 11) {
			Get();
		} else if (la.kind == 12 || la.kind == 13) {
			if (la.kind == 12) {
				Get();
			} else {
				Get();
			}
			op = Op.DIV; 
		} else if (la.kind == 14 || la.kind == 15) {
			if (la.kind == 14) {
				Get();
			} else {
				Get();
			}
			op = Op.MOD; 
		} else SynErr(53);
	}

	void ProcDecl(string progName) {
		Obj obj; string procName; 
		Expect(16);
		Ident(out procName);
		obj = tab.NewObj(procName, proc, undef);
		if (procName == "main")
		  if (tab.curLevel == 0)
		     tab.mainPresent = true;
		  else SemErr("main not at lexic level 0");
		tab.OpenScope();
		
		Expect(9);
		Expect(10);
		Expect(17);
		while (la.kind == 43 || la.kind == 44) {
			VarDecl();
		}
		while (la.kind == 47) {
			StructInit();
		}
		while (la.kind == 16) {
			ProcDecl(progName);
		}
		if (procName == "main")
		  gen.Label("Main", "Body");
		else {
		  gen.ProcNameComment(procName);
		  gen.Label(procName, "Body");
		}
		
		Stat();
		while (StartOf(2)) {
			Stat();
		}
		Expect(18);
		if (procName == "main") {
		  gen.StopProgram(progName);
		  gen.Enter("Main", tab.curLevel, tab.topScope.nextAdr);
		} else {
		  gen.Return(procName);
		  gen.Enter(procName, tab.curLevel, tab.topScope.nextAdr);
		}
		tab.CloseScope();
		
	}

	void VarDecl() {
		string name; int type; 
		Type(out type);
		Ident(out name);
		tab.NewObj(name, var, type); 
		while (la.kind == 45) {
			Get();
			Ident(out name);
			tab.NewObj(name, var, type); 
		}
		Expect(28);
	}

	void StructInit() {
		string name; Obj obj, NewObject; 
		Expect(47);
		Ident(out name);
		obj = tab.Find(name);
		
		if(!obj.isStruct)
		 SemErr("type struct expected");
		
		Ident(out name);
		NewObject = tab.NewObj(name, str, undef);
		NewObject.isStruct = true;
		obj = obj.next;
		// loop through variables in struct and create copys associated with new struct name
		while(obj.isStruct){
		 NewObject = tab.NewObj(name+obj.name, var, obj.type);
		 //   for: struct Person me;  ||
		 //                           \/
		 // the new obj name is      meage     as in me.age
		 NewObject.isStruct = true;
		 obj = obj.next;
		}
		
		Expect(28);
	}

	void Stat() {
		int type; string name; Obj obj; int reg; 
		switch (la.kind) {
		case 2: {
			Ident(out name);
			obj = tab.Find(name); 
			if (la.kind == 6) {
				Get();
				Ident(out name);
				obj = tab.Find(obj.name+name);
				
			}
			if (la.kind == 27) {
				Get();
				if (obj.kind == constant)
				  SemErr("cannot re-assign constant");
				
				  if (obj.kind != var)
				     SemErr("cannot assign to procedure");
				
				Expr(out reg,
out type);
				Expect(28);
				if (type == obj.type)
				  if (obj.level == 0)
				     gen.StoreGlobal(reg, obj.adr, name);
				else gen.StoreLocal(reg, tab.curLevel-obj.level, obj.adr, name);
				else SemErr("incompatible types"); // missing line
				
			} else if (la.kind == 9) {
				Get();
				Expect(10);
				Expect(28);
				if (obj.kind == proc)
				  gen.Call(name);
				else SemErr("object is not a procedure");
				
			} else if (la.kind == 29) {
				Get();
				int NewReg; 
				if(obj.type != integer)
				 SemErr("type integer expected for post incrementation");
				reg = gen.GetRegister();
				NewReg = gen.GetRegister();
				// store 1 in reg
				gen.LoadConstant(NewReg, 1);
				
				//load value of identifier into next reg
				if(obj.level == 0)
				 gen.LoadGlobal(reg,obj.adr, obj.name);
				else
				 gen.LoadLocal(reg, tab.curLevel-obj.level, obj.adr, name);
				// perform addition
				gen.AddOp(Op.ADD, reg, NewReg);
				
				// store new value back to same address
				if (obj.level == 0)
				 gen.StoreGlobal(reg, obj.adr, name);
				else
				 gen.StoreLocal(reg, tab.curLevel-obj.level, obj.adr, name);
				
				Expect(28);
			} else if (la.kind == 30) {
				Get();
				int NewReg; 
				if(obj.type != integer)
				 SemErr("type integer expected for post decrementaion");
				reg = gen.GetRegister();
				NewReg = gen.GetRegister();
				// store 1 in reg
				gen.LoadConstant(NewReg, 1);
				
				//load value of identifier into next reg
				if(obj.level == 0)
				 gen.LoadGlobal(reg,obj.adr, obj.name);
				else
				 gen.LoadLocal(reg, tab.curLevel-obj.level, obj.adr, name);
				// perform subtraction
				gen.AddOp(Op.SUB, reg, NewReg);
				
				// store new value back to same address
				if (obj.level == 0)
				 gen.StoreGlobal(reg, obj.adr, name);
				else
				 gen.StoreLocal(reg, tab.curLevel-obj.level, obj.adr, name);
				
				Expect(28);
			} else SynErr(54);
			break;
		}
		case 31: {
			Get();
			int l1, l2; l1 = 0; 
			Expr(out reg,
out type);
			if (type == boolean) {
			  l1 = gen.NewLabel();
			  gen.BranchFalse(l1);
			}
			else SemErr("boolean type expected");
			
			Stat();
			l2 = gen.NewLabel();
			gen.Branch(l2);
			gen.Label(l1);
			
			if (la.kind == 32) {
				Get();
				Stat();
			}
			gen.Label(l2); 
			break;
		}
		case 33: {
			Get();
			int l1, l2;
			l1 = gen.NewLabel();
			gen.Label(l1); l2=0;
			
			Expr(out reg,
out type);
			if (type == boolean) {
			  l2 = gen.NewLabel();
			  gen.BranchFalse(l2);
			}
			else SemErr("boolean type expected");
			
			Stat();
			gen.Branch(l1);
			gen.Label(l2);
			
			break;
		}
		case 34: {
			Get();
			Expect(9);
			Stat();
			int l1, l2;
			l1 = gen.NewLabel();
			gen.Label(l1);  // label to loop
			l2 = 0;
			
			Expr(out reg,
out type);
			Expect(28);
			if (type == boolean){
			 l2 = gen.NewLabel();
			 gen.BranchFalse(l2);
			}
			else SemErr("boolean type expected"); // must be comparison in for loop
			
			Stat();
			Expect(10);
			Expect(17);
			while (StartOf(2)) {
				Stat();
			}
			Expect(18);
			gen.Branch(l1);
			gen.Label(l2);  // label to exit
			
			break;
		}
		case 35: {
			Get();
			Ident(out name);
			Expect(28);
			obj = tab.Find(name);
			if (obj.type == integer) {
			  gen.ReadInteger();
			  if (obj.level == 0)
			     gen.StoreGlobal(0, obj.adr, name);
			  else gen.StoreLocal(0, tab.curLevel-obj.level, obj.adr, name);
			}
			else SemErr("integer type expected");
			
			break;
		}
		case 36: {
			Get();
			string text; 
			if (StartOf(3)) {
				Expr(out reg,
out type);
				switch (type) {
				  case integer: gen.WriteInteger(reg, false);
				                break;
				  case boolean: gen.WriteBoolean(false);
				                break;
				}
				
			} else if (la.kind == 3) {
				String(out text);
				gen.WriteString(text); 
			} else SynErr(55);
			Expect(28);
			break;
		}
		case 37: {
			Get();
			Expr(out reg,
out type);
			switch (type) {
			  case integer: gen.WriteInteger(reg, true);
			                break;
			  case boolean: gen.WriteBoolean(true);
			                break;
			}
			
			Expect(28);
			break;
		}
		case 17: {
			Get();
			tab.OpenSubScope(); 
			while (la.kind == 43 || la.kind == 44) {
				VarDecl();
			}
			Stat();
			while (StartOf(2)) {
				Stat();
			}
			Expect(18);
			tab.CloseSubScope(); 
			break;
		}
		case 38: {
			Get();
			Expect(9);
			int breakLabel, switchReg, caseReg; breakLabel = 0;
			breakLabel = gen.NewLabel();
			
			Expr(out switchReg,
out type);
			Expect(10);
			Expect(17);
			while (la.kind == 39) {
				gen.GetRegister(); 
				Get();
				Expr(out caseReg,
out type);
				Expect(26);
				int l1 = 0;
				l1 = gen.NewLabel();
				gen.RelOp(Op.EQU, caseReg, switchReg);
				gen.BranchFalse(l1);
				
				while (StartOf(2)) {
					Stat();
				}
				if (la.kind == 40) {
					Get();
					Expect(28);
					gen.Branch(breakLabel); 
				}
				gen.Label(l1); 
			}
			if (la.kind == 41) {
				Get();
				Expect(26);
				while (StartOf(2)) {
					Stat();
				}
			}
			Expect(18);
			gen.Label(breakLabel); 
			break;
		}
		default: SynErr(56); break;
		}
	}

	void Term(out int reg,        // load value of Term into register
out int type) {
		int typeR, regR; Op op; 
		Primary(out reg,
out type);
		while (StartOf(4)) {
			MulOp(out op);
			Primary(out regR,
out typeR);
			if (type == integer && typeR == integer)
			  gen.MulOp(op, reg, regR);
			else SemErr("integer type expected");
			
		}
	}

	void Tastier() {
		string progName; 
		Expect(42);
		Ident(out progName);
		tab.OpenScope(); 
		Expect(17);
		while (la.kind == 46) {
			ConstDef();
		}
		while (la.kind == 43 || la.kind == 44) {
			VarDecl();
		}
		while (la.kind == 47) {
			StructDecl();
		}
		while (la.kind == 16) {
			ProcDecl(progName);
		}
		tab.CloseScope(); 
		Expect(18);
	}

	void ConstDef() {
		string name; int reg, type; Obj obj; 
		Expect(46);
		Type(out type);
		Ident(out name);
		obj = tab.NewObj(name, constant, type); 
		Expect(19);
		Primary(out reg,
out type);
		if (type == obj.type)
		  if (obj.level == 0)
		     gen.StoreGlobal(reg, obj.adr, name);
		  else SemErr("Cannot define constant locally");
		else SemErr("incompatible types");
		
		Expect(28);
	}

	void StructDecl() {
		string name; int reg; Obj obj;
		Expect(47);
		Ident(out name);
		reg = gen.GetRegister();
		obj = tab.NewObj(name, str, undef);
		
		Expect(17);
		while (la.kind == 43 || la.kind == 44) {
			StructVarDecl();
		}
		Expect(48);
		obj.isStruct = true;
		// must be stored globally
		if(obj.level ==0)
		  gen.StoreGlobal(reg, obj.adr, obj.name);
		else
		  SemErr("Cannot store struct locally");
		
	}

	void Type(out int type) {
		type = undef; 
		if (la.kind == 43) {
			Get();
			type = integer; 
		} else if (la.kind == 44) {
			Get();
			type = boolean; 
		} else SynErr(57);
	}

	void StructVarDecl() {
		string name; int type; Obj obj; 
		Type(out type);
		Ident(out name);
		obj = tab.NewObj(name, var, type);
		// same as previous VarDecl except SymTab.cs was edited
		// to contain the boolean "isStruct" which determines if the Ident
		// is involved with a struct
		obj.isStruct = true;
		
		while (la.kind == 45) {
			Get();
			Ident(out name);
			obj = tab.NewObj(name, var, type);
			obj.isStruct = true;
			
		}
		Expect(28);
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		Tastier();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,T,T,x, x,T,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
		{x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x}

	};
} // end Parser


public class Errors {
	public int count = 0;                                    // number of errors detected
    public System.IO.TextWriter errorStream = Console.Error; // error messages go to this stream - was Console.Out DMA
    public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public virtual void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "number expected"; break;
			case 2: s = "ident expected"; break;
			case 3: s = "string expected"; break;
			case 4: s = "\"+\" expected"; break;
			case 5: s = "\"-\" expected"; break;
			case 6: s = "\".\" expected"; break;
			case 7: s = "\"true\" expected"; break;
			case 8: s = "\"false\" expected"; break;
			case 9: s = "\"(\" expected"; break;
			case 10: s = "\")\" expected"; break;
			case 11: s = "\"*\" expected"; break;
			case 12: s = "\"div\" expected"; break;
			case 13: s = "\"DIV\" expected"; break;
			case 14: s = "\"mod\" expected"; break;
			case 15: s = "\"MOD\" expected"; break;
			case 16: s = "\"void\" expected"; break;
			case 17: s = "\"{\" expected"; break;
			case 18: s = "\"}\" expected"; break;
			case 19: s = "\"=\" expected"; break;
			case 20: s = "\"<\" expected"; break;
			case 21: s = "\">\" expected"; break;
			case 22: s = "\"!=\" expected"; break;
			case 23: s = "\"<=\" expected"; break;
			case 24: s = "\">=\" expected"; break;
			case 25: s = "\"?\" expected"; break;
			case 26: s = "\":\" expected"; break;
			case 27: s = "\":=\" expected"; break;
			case 28: s = "\";\" expected"; break;
			case 29: s = "\"++\" expected"; break;
			case 30: s = "\"--\" expected"; break;
			case 31: s = "\"if\" expected"; break;
			case 32: s = "\"else\" expected"; break;
			case 33: s = "\"while\" expected"; break;
			case 34: s = "\"for\" expected"; break;
			case 35: s = "\"read\" expected"; break;
			case 36: s = "\"write\" expected"; break;
			case 37: s = "\"writeln\" expected"; break;
			case 38: s = "\"switch\" expected"; break;
			case 39: s = "\"case\" expected"; break;
			case 40: s = "\"break\" expected"; break;
			case 41: s = "\"default\" expected"; break;
			case 42: s = "\"program\" expected"; break;
			case 43: s = "\"int\" expected"; break;
			case 44: s = "\"bool\" expected"; break;
			case 45: s = "\",\" expected"; break;
			case 46: s = "\"const\" expected"; break;
			case 47: s = "\"struct\" expected"; break;
			case 48: s = "\"};\" expected"; break;
			case 49: s = "??? expected"; break;
			case 50: s = "invalid AddOp"; break;
			case 51: s = "invalid RelOp"; break;
			case 52: s = "invalid Primary"; break;
			case 53: s = "invalid MulOp"; break;
			case 54: s = "invalid Stat"; break;
			case 55: s = "invalid Stat"; break;
			case 56: s = "invalid Stat"; break;
			case 57: s = "invalid Type"; break;

			default: s = "error " + n; break;
		}
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}

	public virtual void SemErr (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
		count++;
	}
	
	public virtual void SemErr (string s) {
		errorStream.WriteLine(s);
		count++;
	}
	
	public virtual void Warning (int line, int col, string s) {
		errorStream.WriteLine(errMsgFormat, line, col, s);
	}
	
	public virtual void Warning(string s) {
		errorStream.WriteLine(s);
	}
} // Errors


public class FatalError: Exception {
	public FatalError(string m): base(m) {}
}
}