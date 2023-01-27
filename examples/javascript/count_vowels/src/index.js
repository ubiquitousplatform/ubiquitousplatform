const vowels = ["a", "e", "i", "o", "u", "y"];
export const count_vowels = () => {
  const input = Host.inputString();
  let vowelCount = 0;
  for (var i = 0; i < input.length; i++) {
    for (var j = 0; j < vowels.length; j++) {
      if (input.charAt(i) === vowels[j]) {
        vowelCount += 1;
        break;
      }
    }
  }
  Host.outputString(JSON.stringify({ count: vowelCount }));
};
