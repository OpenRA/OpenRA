#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Primitives
{
	public class PredictedCachedTransform<T, U>
	{
		readonly Func<T, U> transform;

		bool initialized;
		T lastInput;
		U lastOutput;

		bool predicted;
		U prediction;

		public PredictedCachedTransform(Func<T, U> transform)
		{
			this.transform = transform;
		}

		public void Predict(U value)
		{
			predicted = true;
			prediction = value;
		}

		public U Update(T input)
		{
			if ((predicted || initialized) && ((input == null && lastInput == null) || (input != null && input.Equals(lastInput))))
				return predicted ? prediction : lastOutput;

			predicted = false;
			initialized = true;
			lastInput = input;
			lastOutput = transform(input);

			return lastOutput;
		}
	}
}
