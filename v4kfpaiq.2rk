openapi: 3.0.0
info:
  title: JSONPlaceholder API
  description: Fake REST API for testing and prototyping.
  version: 1.0.0

servers:
  - url: https://jsonplaceholder.typicode.com

paths:
  /posts:
    get:
      summary: Get all posts
      responses:
        '200':
          description: A list of posts
    post:
      summary: Create a new post
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/PostRequest'
      responses:
        '201':
          description: Post created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PostResponse'

  /posts/{id}:
    get:
      summary: Get a post by ID
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
      responses:
        '200':
          description: Post found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PostResponse'

    put:
      summary: Update a post
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/PostRequest'
      responses:
        '200':
          description: Post updated
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PostResponse'

    delete:
      summary: Delete a post
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
      responses:
        '200':
          description: Post deleted
    
    patch:
      summary: Partially update a post
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              properties:
                title:
                  type: string
                body:
                  type: string
      responses:
        '200':
          description: Post updated
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/PostResponse'


components:
  schemas:
    PostRequest:
      type: object
      properties:
        title:
          type: string
        body:
          type: string
        userId:
          type: integer
      required:
        - title
        - body
        - userId

    PostResponse:
      type: object
      properties:
        id:
          type: integer
        title:
          type: string
        body:
          type: string
        userId:
          type: integer
