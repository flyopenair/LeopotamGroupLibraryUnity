//-------------------------------------------------------
// LeopotamGroupLibrary for unity3d License
// Copyright (c) 2012-2016 Leopotam <leopotam@gmail.com>
//-------------------------------------------------------
// Autogenerated with Coco/R, dont change it manually.
//-------------------------------------------------------

using System.Collections.Generic;
using LeopotamGroup.Common;


using System;
namespace LeopotamGroup.Scripting.Internal {


class Parser {
	public const int _EOF = 0;
	public const int _IDENT = 1;
	public const int _NUMBER = 2;
	public const int _STRING = 3;
	public const int maxT = 23;

	const bool _T = true;
	const bool _x = false;
	const int _minErrDist = 2;

	Scanner _scanner;
	public readonly Vars Vars;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = _minErrDist;

public readonly List<ScriptVar> CallParams = new List<ScriptVar>(8);
	public int CallParamsOffset;
	public ScriptVar RetVal;

	readonly List<string> _paramList = new List<string>(8);
	bool _isParsing;
	bool _isReturned;

	public void Reset () {
        Vars.Reset ();
        _paramList.Clear();
        CallParams.Clear();
        CallParamsOffset = 0;
        _isReturned = false;
        _isParsing = false;
    }

	public string CallFunction() {
		la = Scanner.EmptyToken;
		_isReturned = false;
		RetVal = new ScriptVar();
		try {
			Get();
			Block();
		} catch (Exception ex) {
			return ex.Message;
		}

		return null;
	}
	bool NotBrace() {
		return _scanner.Peek().val != "(";
	}
//----------------------------------------------------------------------------------------------------------------------


	public Parser(ScriptVM vm, Scanner scanner) {
		_scanner = scanner;
		Vars = new Vars(vm);
	}

	void SynErr (int n) {
		if (errDist >= _minErrDist) {
			Errors.SynErr(la.line, la.col, n);
		}
		errDist = 0;
	}

	public void SemErr (string msg) {
		if (errDist >= _minErrDist) {
			Errors.SemErr(t.line, t.col, msg);
		}
		errDist = 0;
	}
	
	void Get () {
		while (true) {
			t = la;
			la = _scanner.Scan();
			if (la.kind <= maxT) {
				errDist++;
				break;
			}

			la = t;
		}
	}
	
	void Expect (int n) {
		if (la.kind==n) {
			Get();
		} else {
			SynErr(n);
		}
	}
	
	bool StartOf (int s) {
		return set[s, la.kind];
	}
	
	void ExpectWeak (int n, int follow) {
		if (la.kind == n) {
			Get();
		}
		else {
			SynErr(n);
			while (!StartOf(follow)) {
				Get();
			}
		}
	}

	bool WeakSeparator(int n, int syFol, int repFol) {
		var kind = la.kind;
		if (kind == n) {
			Get();
			return true;
		} else if (StartOf(repFol)) {return false;}
		else {
			SynErr(n);
			while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
				Get();
				kind = la.kind;
			}
			return StartOf(syFol);
		}
	}
	
	void ScriptVM() {
		_isParsing = true; 
		while (la.kind == 4) {
			Function();
		}
		_isParsing = false; 
	}

	void Function() {
		_paramList.Clear(); 
		Expect(4);
		Expect(1);
		var funcName = t.val; 
		Expect(5);
		if (la.kind == 1) {
			Get();
			_paramList.Add(t.val);  
			while (la.kind == 6) {
				Get();
				Expect(1);
				_paramList.Add(t.val);  
			}
		}
		Expect(7);
		if (Vars.IsFunctionExists(funcName) || Vars.IsHostFunctionExists(funcName)) {
		SemErr(string.Format("Function '{0}' already declared", funcName));
		}
		Vars.RegisterFunction(funcName, _scanner.PC, _paramList);
		
		Block();
	}

	void Block() {
		
		Expect(8);
		Seq();
		if (!_isParsing) {
		if (_isReturned) {
			return;
		}
		}
		
		Expect(9);
	}

	void Seq() {
		
		while (StartOf(1)) {
			if (la.kind == 21) {
				If();
			} else if (la.kind == 10) {
				Get();
				if (StartOf(2)) {
					Expr(out RetVal);
				}
				Expect(11);
				if (!_isParsing) {
				_isReturned = true;
				return;
				}
				
			} else {
				Decl();
			}
		}
	}

	void If() {
		
		Expect(21);
		Expect(5);
		ScriptVar v; 
		Expr(out v);
		var isValid = v.AsNumber != 0f; var isSwitched = false; 
		Expect(7);
		if (!_isParsing) {
		isSwitched = !isValid;
		if (isSwitched) {
			_isParsing = true;
		}
		}
		
		Block();
		if (isSwitched) {
		isSwitched = false;
		_isParsing = false;
		if (_isReturned) {
			return;
		}
		}
		
		if (la.kind == 22) {
			Get();
			if (!_isParsing) {
			isSwitched = isValid;
			if (isSwitched) {
				_isParsing = true;
			}
			}
			
			Block();
			if (isSwitched) {
			isSwitched = false;
			_isParsing = false;
			if (_isReturned) {
				return;
			}
			}
			
		}
	}

	void Expr(out ScriptVar v) {
		ScriptVar b; int mode; 
		Expr2(out v);
		while (la.kind == 12 || la.kind == 13 || la.kind == 14) {
			if (la.kind == 12) {
				Get();
				mode = 0; 
			} else if (la.kind == 13) {
				Get();
				mode = 1; 
			} else {
				Get();
				mode = 2; 
			}
			Expr2(out b);
			if (!_isParsing) {
			switch (mode) {
				case 0:
					if (!v.IsNumber || !b.IsNumber) {
						SemErr("'<' operator can be applied to numbers only");
					}
					v.AsNumber = v.AsNumber < b.AsNumber ? 1f : 0f;
					break;
				case 1:
					if (v.Type != b.Type) {
						v.AsNumber = 0f;
					} else {
						v.AsNumber = v.IsNumber ? (v.AsNumber == b.AsNumber ? 1f : 0f) : (v.AsString == b.AsString ? 1f : 0f);
					}
					break;
				case 2:
					if (!v.IsNumber || !b.IsNumber) {
						SemErr("'>' operator can be applied to numbers only");
					}
					v.AsNumber = v.AsNumber > b.AsNumber ? 1f : 0f;
					break;
			}
			}
			
		}
	}

	void Decl() {
		var isNew = false; var type = ScriptVarType.Undefined; ScriptVar v; var isCalling = false; string name = null; var isAssigned = false; 
		if (la.kind == 19) {
			Get();
			isNew = true; 
		}
		if (NotBrace()) {
			Expect(1);
			name = t.val;
			if (!_isParsing) {
			var isExists = Vars.IsFunctionExists(name) || Vars.IsHostFunctionExists(name);
			if (isExists) {
				SemErr(string.Format("Function '{0}' exists and cant be assigned as variable", name));
			}
			isExists = Vars.IsVarExists(name);
			if (isNew && isExists) {
				SemErr(string.Format("Variable '{0}' already declared", name));
			}
			if (isExists) {
				type = Vars.GetVar(name).Type;
			}
			if (!isNew && !isExists) {
				SemErr(string.Format("Variable '{0}' not declared", name));
			}
			}
			
		} else if (StartOf(2)) {
			if (isNew) { SemErr("Invalid usage of variable declaration"); } 
			Expr(out v);
			isCalling = true; 
		} else SynErr(24);
		if (la.kind == 20) {
			if (isCalling) { SemErr("Only variable can be assigned, not expression"); } 
			Get();
			Expr(out v);
			if (!_isParsing) {
			if (type != ScriptVarType.Undefined && v.Type != type) {
				SemErr(string.Format("Variable '{0}' type cant be changed", name));
			}
			Vars.RegisterVar(name, v);
			} else {
			isAssigned = true;
			}
			
		}
		Expect(11);
		if (_isParsing && isNew && !isCalling && !isAssigned) { SemErr(string.Format("Variable '{0}' should be initialized", name)); } 
	}

	void Expr2(out ScriptVar v) {
		ScriptVar b; bool isSub; 
		Expr3(out v);
		while (la.kind == 15 || la.kind == 16) {
			if (la.kind == 15) {
				Get();
				isSub = false; 
			} else {
				Get();
				isSub = true; 
			}
			Expr3(out b);
			if (!_isParsing) {
			if (v.IsString || b.IsString) {
				if (isSub) {
					SemErr("Operator '-' cant be applied to strings");
				} else {
					v.AsString = v.AsString + b.AsString;
				}
			} else {
				if (v.IsNumber || b.IsNumber) {
					v.AsNumber = v.AsNumber + (isSub ? -b.AsNumber : b.AsNumber);
				}
			}
			}
			
		}
	}

	void Expr3(out ScriptVar v) {
		ScriptVar b; 
		Expr4(out v);
		while (la.kind == 17 || la.kind == 18) {
			bool isDiv; 
			if (la.kind == 17) {
				Get();
				isDiv = false; 
			} else {
				Get();
				isDiv = true; 
			}
			Expr4(out b);
			if (!_isParsing) {
			if (!v.IsNumber || !b.IsNumber) {
				SemErr(string.Format("Operator '{0}' cant be applied to numbers only", isDiv ? '/' : '*'));
			}
			v.AsNumber = isDiv ? (b.AsNumber == 0f ? 0f : v.AsNumber / b.AsNumber) : v.AsNumber * b.AsNumber;
			}
			
		}
	}

	void Expr4(out ScriptVar v) {
		v = new ScriptVar(); var isNegative = false; 
		if (la.kind == 16) {
			Get();
			isNegative = true; 
		}
		if (la.kind == 5) {
			Get();
			Expr(out v);
			Expect(7);
		} else if (la.kind == 1 || la.kind == 2 || la.kind == 3) {
			Term(out v);
		} else SynErr(25);
		if (!_isParsing) {
		if (isNegative) {
			if (v.IsNumber) {
					v.AsNumber = -v.AsNumber;
			} else {
				SemErr("Minus can be applied only on numbers");
			}
		}
		}
		
	}

	void Term(out ScriptVar v) {
		v = new ScriptVar(); var callOffset = int.MaxValue; var isCall = false; 
		if (la.kind == 1) {
			Get();
			var identName = t.val; 
			if (la.kind == 5) {
				Get();
				ScriptVar p; if (!_isParsing) { isCall = true; callOffset = CallParams.Count; } 
				if (StartOf(2)) {
					Expr(out p);
					if (!_isParsing) { CallParams.Add(p); } 
					while (la.kind == 6) {
						Get();
						Expr(out p);
						if (!_isParsing) { CallParams.Add(p); } 
					}
				}
				Expect(7);
			}
			if (!_isParsing) {
			bool isExists;
			if (isCall) {
				isExists = Vars.IsHostFunctionExists(identName);
				if (!isExists) {
					SemErr(string.Format("Cant find host function with name '{0}'", identName));
				}
				CallParamsOffset = callOffset;
				v = Vars.CallHostFunction(identName);
				while (CallParams.Count > callOffset) {
					CallParams.RemoveAt(CallParams.Count - 1);
				}
			} else {
				if (!Vars.IsVarExists(t.val)) {
					SemErr(string.Format("Variable '{0}' not found", identName));
				}
				v = Vars.GetVar(identName);
			}
			}
			
		} else if (la.kind == 2) {
			Get();
			if (!_isParsing) {
			v.AsNumber = t.val.ToFloatUnchecked();
			}
			
		} else if (la.kind == 3) {
			Get();
			if (!_isParsing) {
			v.AsString = t.val;
			}
			
		} else SynErr(26);
	}


	public string Parse() {
		la = Scanner.EmptyToken;
		try {
			Get();
		ScriptVM();
		Expect(0);

		} catch (Exception ex) {
			return ex.Message;
		}
		return null;
	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x},
		{_x,_T,_T,_T, _x,_T,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _T,_x,_x,_T, _x,_T,_x,_x, _x},
		{_x,_T,_T,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x}

	};
}

static class Errors {
	const string ErrFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

	public static void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "IDENT expected"; break;
			case 2: s = "NUMBER expected"; break;
			case 3: s = "STRING expected"; break;
			case 4: s = "\"function\" expected"; break;
			case 5: s = "\"(\" expected"; break;
			case 6: s = "\",\" expected"; break;
			case 7: s = "\")\" expected"; break;
			case 8: s = "\"{\" expected"; break;
			case 9: s = "\"}\" expected"; break;
			case 10: s = "\"return\" expected"; break;
			case 11: s = "\";\" expected"; break;
			case 12: s = "\"<\" expected"; break;
			case 13: s = "\"==\" expected"; break;
			case 14: s = "\">\" expected"; break;
			case 15: s = "\"+\" expected"; break;
			case 16: s = "\"-\" expected"; break;
			case 17: s = "\"*\" expected"; break;
			case 18: s = "\"/\" expected"; break;
			case 19: s = "\"var\" expected"; break;
			case 20: s = "\"=\" expected"; break;
			case 21: s = "\"if\" expected"; break;
			case 22: s = "\"else\" expected"; break;
			case 23: s = "??? expected"; break;
			case 24: s = "invalid Decl"; break;
			case 25: s = "invalid Expr4"; break;
			case 26: s = "invalid Term"; break;

			default: s = "error " + n; break;
		}
		SemErr(line, col, s);
	}

	public static void SemErr (int line, int col, string msg) {
		throw new Exception (string.Format(ErrFormat, line, col, msg));
	}
}}