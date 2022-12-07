﻿using System.Collections.ObjectModel;

namespace sena.AST;

public class Root : INode
{
	public readonly ReadOnlyCollection<IStatement> statements;

	public Root(List<IStatement> statements)
	{
		this.statements = statements.AsReadOnly();
	}
}
