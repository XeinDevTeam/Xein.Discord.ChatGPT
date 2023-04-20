# Python program to generate word vectors using Word2Vec

# importing all necessary modules
from nltk.tokenize import sent_tokenize, word_tokenize
import warnings

warnings.filterwarnings(action = 'ignore')

import gensim
from gensim.models import Word2Vec

# Reads ‘alice.txt’ file
sample = open("11-0.txt", "r+", encoding="utf8")
s = sample.read()

# Replaces escape character with space
f = s.replace("\n", " ")

data = []

# iterate through each sentence in the file
for i in sent_tokenize(f):
	temp = []
	
	# tokenize the sentence into words
	for j in word_tokenize(i):
		temp.append(j.lower())

	data.append(temp)

# Create CBOW model
model1 = gensim.models.Word2Vec(data, min_count = 1, vector_size = len(data), window = 5, workers=4)

# Print results
print("Cosine similarity between 'alice' and 'wonderland' - CBOW : ",
	model1.wv.similarity('alice', 'wonderland'))
	
print("Cosine similarity between 'alice' and 'machines' - CBOW : ",
	model1.wv.similarity('alice', 'machines'))

print("Cosine similarity between 'alice' and 'cat' - CBOW : ",
	model1.wv.similarity('alice', 'cat'))

model1.save("testmodel")